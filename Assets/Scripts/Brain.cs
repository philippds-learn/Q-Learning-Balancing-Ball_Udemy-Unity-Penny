using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Replay
{
    public List<double> states;
    public double reward;

    public Replay(double xr, double ballz, double ballvx, double r)
    {
        this.states = new List<double>();
        this.states.Add(xr);
        this.states.Add(ballz);
        this.states.Add(ballvx);
        this.reward = r;
    }
}

public class Brain : MonoBehaviour
{
    public GameObject ball;                             // object to monitor

    ANN ann;

    float reward = 0.0f;                                // reward to associate with actions
    List<Replay> replayMemory = new List<Replay>();     // memory - list of past actions and rewards
    int mCapacity = 10000;                              // memory capacity

    float discount = 0.99f;                             // how much future states affect rewards
    float exploreRate = 100.0f;                         // chance of picking random action
    float maxExploreRate = 100.0f;                      // max chance value
    float minExploreRate = 0.01f;                       // min chance value
    float exploreDecay = 0.0001f;                       // chance decay amount for each update

    Vector3 ballStartPos;                               // record start position of object
    int failCount = 0;                                  // count when the ball is dropped
    float tiltSpeed = 0.5f;                             // max angle to apply to tilting each update
                                                        // make sure this is large enough so that the q value
                                                        // multiplied by it is enough to recover balance
                                                        // when the ball gets a good speed up

    float timer = 0;                                    // timer to keep track of balancing
    float maxBalanceTime = 0;                           // record time ball is kept balanced

    // Start is called before the first frame update
    void Start()
    {
        this.ann = new ANN(3, 2, 1, 6, 0.2f);
        this.ballStartPos = this.ball.transform.position;
        Time.timeScale = 5.0f;
    }

    GUIStyle guiStyle = new GUIStyle();
    private void OnGUI()
    {
        this.guiStyle.fontSize = 25;
        this.guiStyle.normal.textColor = Color.white;
        GUI.BeginGroup(new Rect(10, 10, 600, 150));
        GUI.Box(new Rect(0, 0, 140, 140), "Stats", this.guiStyle);
        GUI.Label(new Rect(10, 25, 500, 30), "Fails: " + this.failCount, this.guiStyle);
        GUI.Label(new Rect(10, 50, 500, 30), "Decay Rate: " + this.exploreRate, this.guiStyle);
        GUI.Label(new Rect(10, 75, 500, 30), "Last Best Balance: " + this.maxBalanceTime, this.guiStyle);
        GUI.Label(new Rect(10, 100, 500, 30), "This Balance: " + this.timer, this.guiStyle);
        GUI.EndGroup();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("space"))
        {
            ResetBall();
        }
    }

    void FixedUpdate()
    {
        this.timer += Time.deltaTime;
        List<double> states = new List<double>();
        List<double> qs = new List<double>();

        states.Add(this.transform.rotation.x);
        states.Add(this.ball.transform.position.z);
        states.Add(this.ball.GetComponent<Rigidbody>().angularVelocity.x);

        qs = SoftMax(this.ann.CalcOutput(states));
        double maxQ = qs.Max();
        int maxQIndex = qs.ToList().IndexOf(maxQ);
        this.exploreRate = Mathf.Clamp(this.exploreRate - this.exploreDecay, this.minExploreRate, this.maxExploreRate);

        /*
        if(Random.Range(0, 100) < this.exploreRate)
        {
            maxQIndex = Random.Range(0, 2);
        }
        */

        if(maxQIndex == 0)
        {
            this.transform.Rotate(Vector3.right, this.tiltSpeed + (float)qs[maxQIndex]);
        }
        else if(maxQIndex == 1)
        {
            this.transform.Rotate(Vector3.left, this.tiltSpeed + (float)qs[maxQIndex]);
        }

        if(this.ball.GetComponent<BallState>().dropped)
        {
            this.reward = -1.0f;
        }
        else
        {
            this.reward = 0.1f;
        }

        Replay lastMemory = new Replay(this.transform.rotation.x, this.ball.transform.position.z, this.ball.GetComponent<Rigidbody>().angularVelocity.x, this.reward);

        if(this.replayMemory.Count > this.mCapacity)
        {
            this.replayMemory.RemoveAt(0);
        }

        this.replayMemory.Add(lastMemory);

        if(this.ball.GetComponent<BallState>().dropped)
        {
            for(int i = replayMemory.Count - 1; i >= 0; i--)
            {
                List<double> toutputsOld = new List<double>();
                List<double> toutputsNew = new List<double>();
                toutputsOld = SoftMax(this.ann.CalcOutput(this.replayMemory[i].states));

                double maxQOld = toutputsOld.Max();
                int action = toutputsOld.ToList().IndexOf(maxQOld);

                double feedback;
                if(i == this.replayMemory.Count - 1 || this.replayMemory[i].reward == -1)
                {
                    feedback = this.replayMemory[i].reward;
                }
                else
                {
                    toutputsNew = SoftMax(this.ann.CalcOutput(this.replayMemory[i + 1].states));
                    maxQ = toutputsNew.Max();
                    feedback = (this.replayMemory[i].reward + this.discount * maxQ);
                }

                toutputsOld[action] = feedback;
                this.ann.Train(this.replayMemory[i].states, toutputsOld);
            }

            if(this.timer > this.maxBalanceTime)
            {
                this.maxBalanceTime = this.timer;
            }

            this.timer = 0;

            this.ball.GetComponent<BallState>().dropped = false;
            this.transform.rotation = Quaternion.identity;
            ResetBall();
            this.replayMemory.Clear();
            this.failCount++;
        }
    }

    void ResetBall()
    {
        this.ball.transform.position = this.ballStartPos;
        this.ball.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        this.ball.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
    }

    List<double> SoftMax(List<double> values)
    {
        double max = values.Max();

        float scale = 0.0f;

        for(int i = 0; i < values.Count; ++i)
        {
            scale += Mathf.Exp((float)(values[i] - max));
        }

        List<double> result = new List<double>();
        for(int i = 0; i < values.Count; ++i)
        {
            result.Add(Mathf.Exp((float)(values[i] - max)) / scale);
        }

        return result;
    }
}
