using System;
using UnityEngine;
using Mirror;
using LastKnightXZ.Utils.CustomFixedUpdate;

public class TickManger : NetworkBehaviour
{  
    /// <summary>
    /// Time frequency for ticks
    /// </summary>
    private static int tickFrequency = 10;
    
    /// <summary>
    /// Time between each ticks
    /// </summary>
    public static double tickDelta => 1d/tickFrequency;

    /// <summary>
    /// Time since first tick started
    /// </summary>
    public static double fixedTickTime{get; private set;}
    
    /// <summary>
    /// Did the tick was evaluated this frame
    /// </summary>
    public static bool isTickFrame{get; private set;}
    
    /// <summary>
    /// Current tick value 
    /// </summary>
    public static uint tick{get; private set;}

    /// <summary>
    /// An event to Trigger collection of PlayerInputs from host/client
    /// </summary>
    public static event Action LocalPlayerInputCollectionEvent;

    /// <summary>
    /// An event to trigger update for GameState Logic other than physics and animation
    /// </summary>
    public static event Action OtherGameStateUpdateEvent;

    public static float PhysicsFixedDeltaTime => Time.fixedDeltaTime;
    public static float PhysicsMaxAllowedTimestep => Time.maximumDeltaTime;
    private static CustomFixedUpdate PhysicsFixedUpdateHandler;

    /// <summary>
    /// An event to trigger update for physics State, to generally change physics parameters 
    /// </summary>
    public static event Action PhysicsFixedUpdateEvent;

    //FixedUpdate For Animator not implemented beacause there is no scripting api/settings to disable internal animation update
    //so animation states now depend on frame rate     
    //public static CustomFixedUpdate myAnimatorFixedUpdate;

    /// <summary>
    /// An event to trigger update for Animator State, to generally change animator parameters and layer values  
    /// </summary>
    public static event Action AnimatorUpdateEvent;


    #region server fields
    
    /// <summary>
    /// An event to trigger, sending clients , targetReconcilltion and entity interpolation messages 
    /// </summary>
    public static event Action ServerMessageEvent;

    /// <summary>
    /// An event to trigger, store new Gamestates
    /// </summary>
    public static event Action ServerGameStateStoreEvent;
    
    #endregion

    #region client fields
    
    /// <summary>
    /// An event to Store client input and gameState for later reconcillation
    /// </summary>
    public static event Action StoreCurrentClientGameStateAndInputEvent;
    
    /// <summary>
    /// An event to send client Input and TicksToGoBackForLagCompensation to server
    /// </summary>
    public static event Action clientMessageEvent;

    /// <summary>
    /// How much ticks we are allowed to wait after which we start showing interpolated player states from server 
    /// </summary>
    public static uint TicksToWaitForEntityInterpolation = 1;
    
    /// <summary>
    /// How much Ticks Needs the server to go back for Lag Compensation
    /// note: As rtt is also influenced by client and server frame rate, here we remove influence of client frame rate
    /// thus getting a closer estimate of time to reach from client to server
    /// </summary>
    public static uint TicksToGoBackForLagCompensation => TicksToWaitForEntityInterpolation + (uint)(Math.Max((NetworkTime.rtt-(Time.deltaTime/2f)),0d)/(tickDelta*2d));

    #endregion
    
    private void Awake() 
    {  
        isTickFrame = false;
        fixedTickTime = 0d; 
        tick = 0;
        PhysicsFixedUpdateHandler = new CustomFixedUpdate(PhysicsFixedDeltaTime,PhysicsMaxAllowedTimestep,PhysicsFixedUpdate);

        #if USING_PHYSICS2D
            Physics2D.simulationMode = SimulationMode2D.Script;
        #endif
        #if USING_PHYSICS3D
            Physics.autoSimulation = false;
        #endif
    }

    private void Update()
    {   
        if(isServer)
        {   
            //host / headless server
            ServerUpdate();
        }
        else
        {   
            //client
            ClientUpdate();
        }
    }

    private static void PhysicsFixedUpdate() 
    {              
        PhysicsFixedUpdateEvent?.Invoke();
        PhysicsStep();
    }

    public static void PhysicsStep()
    {   
        #if USING_PHYSICS2D
            Physics2D.Simulate(Time.fixedDeltaTime);
        #endif
        #if USING_PHYSICS3D
            Physics.Simulate(Time.fixedDeltaTime);
        #endif
    }

    [Server]
    private void ServerUpdate()
    {
        if(NetworkTime.localTime-fixedTickTime>=tickDelta)
        {   
            //generally 0 to 1 ticks  will be evaluated each update as tickrate shouldnot be faster than server framerate, 
            //but incase it exceeds 1 (due to framerate drop,etc) we are forced to skip sending those inbetween ticks as we are limited
            //by frameRate to send messages
            
            fixedTickTime = NetworkTime.localTime - (NetworkTime.localTime%tickDelta);
            tick = (uint)(NetworkTime.localTime/tickDelta);
            isTickFrame = true;
            
            //gameState update
            OtherGameStateUpdateEvent?.Invoke();

            //physicsUpdate till tickTime
            PhysicsFixedUpdateHandler.referenceTime = fixedTickTime;
            PhysicsFixedUpdateHandler.Update(0d);
            
            //animation update till tickTime not done
            //because there is no api to disable internal animation update, and thus not able to implement fixedUpdate
            //here the state corresponds to localTime instead of TickTime 
            //and this state will be more accurate as the framerate is closer to either multiples of tickRate 
            AnimatorUpdateEvent?.Invoke();
            
            //storing current state of game 
            ServerGameStateStoreEvent?.Invoke();

            //sending clients the state evaluated upto tickTime
            ServerMessageEvent?.Invoke();
        }
        else
        {
            isTickFrame = false;
        }

        //for input collection for host 
        LocalPlayerInputCollectionEvent?.Invoke();

        // To process the game state till current time for rendering
        OtherGameStateUpdateEvent?.Invoke();
        PhysicsFixedUpdateHandler.referenceTime = NetworkTime.localTime;
        PhysicsFixedUpdateHandler.Update(0d);
        AnimatorUpdateEvent?.Invoke();

    }
    
    [Client]
    private void ClientUpdate()
    {
        if(NetworkTime.clientTime-fixedTickTime>=tickDelta)
        {   
            
            //generally 0 to 1 ticks  will be evaluated each update as tickrate shouldnot be faster than server framerate, 
            //but incase it exceeds 1 (due to framerate drop,etc) we are forced to skip sending those inbetween ticks as we are limited
            //by frameRate to send messages
            
            fixedTickTime = NetworkTime.clientTime - (NetworkTime.clientTime%tickDelta); 
            tick = (uint)(NetworkTime.clientTime/tickDelta);
            isTickFrame = true;

            //gameState update
            OtherGameStateUpdateEvent?.Invoke();

            //physicsUpdate till tickTime
            PhysicsFixedUpdateHandler.referenceTime = fixedTickTime;
            PhysicsFixedUpdateHandler.Update(0d);
            
            //animation update till tickTime not done
            //because there is no api to disable internal animation update, and thus not able to implement fixedUpdate
            //here the state corresponds to localTime instead of TickTime 
            //and this state will be more accurate as the framerate is closer to either multiples of tickRate 
            AnimatorUpdateEvent?.Invoke();
            
            //collecting fresh input from client
            LocalPlayerInputCollectionEvent?.Invoke();
            
            //sending the fresh input to server
            clientMessageEvent?.Invoke();
            
            //storing gameState evaluated till tick Time (with previous inputs) and the fresh input for this tick
            StoreCurrentClientGameStateAndInputEvent?.Invoke();
        }
        else
        {
            isTickFrame = false;

            //for in between frames between ticks we do not use fresh inputs as server will use the same input for whole tick
            //and a different inbetween input will only lead to mismatch with server and predicted state
            //if you want more responsive behaviour then you need to increase tick rate 
            //LocalPlayerInputCollectionEvent?.Invoke();
            
        }

        //for updating game state corresponding to clientTime to be rendered later
        PhysicsFixedUpdateHandler.referenceTime = NetworkTime.clientTime;
        OtherGameStateUpdateEvent?.Invoke();
        PhysicsFixedUpdateHandler.Update(0d);        
        AnimatorUpdateEvent?.Invoke();        

    }
}

