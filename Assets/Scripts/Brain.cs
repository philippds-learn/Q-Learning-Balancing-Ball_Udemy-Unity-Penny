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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
