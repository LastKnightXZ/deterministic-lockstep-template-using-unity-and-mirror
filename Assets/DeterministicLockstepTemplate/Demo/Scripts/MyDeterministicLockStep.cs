using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.IO;
using LastKnightXZ.NetworkedDataDefinations;
using LastKnightXZ.Utils.CustomFixedUpdate;
using LastKnightXZ.Utils.QueuedCoroutine;

#if USING_PHYSICS2D
[RequireComponent(typeof(Rigidbody2D))] 
#endif
#if USING_PHYSICS3D
[RequireComponent(typeof(Rigidbody))] 
#endif
#if USING_MECHANIM
[RequireComponent(typeof(Animator))]
#endif 
public class MyDeterministicLockStep : NetworkBehaviour
{
    [Header("DeterministicLockstepConfig")] 
    [SerializeField]protected uint PlayerStateBufferSize;


    #region  References to be used in user defined functions
    
    private Rigidbody2D myRigidbody2D;
    private Rigidbody myRigidbody;
    private Animator myAnimator;
    private GameState myCurrentGameState;
 
    #endregion

    #region Server Player References for other Player field
    
    private static Dictionary<Transform, Rigidbody2D> ServerAllRigidBody2DList = new Dictionary<Transform, Rigidbody2D>();
    private static Dictionary<Transform, Rigidbody> ServerAllRigidBodyList = new Dictionary<Transform, Rigidbody>();
    private static Dictionary<Transform, Animator> ServerAllAnimatorList = new Dictionary<Transform, Animator>();
    private static Dictionary<Transform, GameState> ServerAllCurrentGameStateList = new Dictionary<Transform, GameState>();    
    
    private static Dictionary<Transform, GameState[]> ServerAllGameStateList = new Dictionary<Transform, GameState[]>();
    private static Dictionary<Transform, Action<GameState>> ServerApplyGameStateActionList = new Dictionary<Transform, Action<GameState>>();

    #endregion 

    #region  LocalPlayer fields including host

    private PlayerInput localPlayerInput;
    
    #endregion

    #region  LocalPlayer fields excluding host

    private uint clientLocalPlayerLastAcceptedState = 0;
    private PlayerInputMsg clientLocalPlayerInputMsg;
    private PlayerInput[] clientLocalPlayerInputBuffer;
    private GameState[] clientLocalPlayerGameStateBuffer;
    private CustomFixedUpdate clientLocalPlayerReconciliationPhysicsFixedUpdateHandler;
    
    #endregion

    #region client Fields excluding localPLayer

    private uint clientOtherPlayerLastAcceptedState = 0;
    private Queue<GameState> clientOtherPlayerGameStatesFromServerQueue;
    private QueuedCoroutine clientOtherPlayerGameStateInterpolationQueue;
    private Queue<GameState> clientOtherPlayerGameStateReferencesForInterpolationQueue;
    private Coroutine clientOtherPlayerActiveCoroutineReference;
    
    #endregion

    #region  serverPlayer fields excluding host
    
    private PlayerInputMsg ServerMsgFromClient;
    
    #endregion

    #region  serverPlayer fields including host
    
    private GameState ServerGameStateForclient;
    private GameState[] ServerGameStateList;
    private uint TicksToGoBackForLagCompensationFromClient = 0;

    #endregion

    private InputManager localPlayerInputManager;
    private CircleCollider2D mycollider;
    private LayerMask Ground;
    private LayerMask Player;
    [SerializeField]private GameObject DrawRaycastPrefab;
 
    private void Awake() 
    {   
        localPlayerInputManager = new InputManager();
        //rigidBody is made dynamic on start function inside relevant player objects 
        #if USING_PHYSICS2D
            myRigidbody2D = GetComponent<Rigidbody2D>();
            myRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        #endif
        #if USING_PHYSICS3D
            myRigidbody = GetComponent<Rigidbody>(); 
            myRigidbody.isKinematic = true;
        #endif
        #if USING_MECHANIM
        myAnimator = GetComponent<Animator>();
        #endif
    
        myCurrentGameState = new GameState(myAnimator);


        mycollider = GetComponent<CircleCollider2D>();
        Ground = LayerMask.GetMask("Ground");
        Player = LayerMask.GetMask("Player");
    }

    public override void OnStartLocalPlayer() 
    {   
        base.OnStartLocalPlayer();

        #if USING_PHYSICS2D
            myRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        #endif
        #if USING_PHYSICS3D
            myRigidbody.isKinematic = false;
        #endif

        localPlayerInput = new PlayerInput();

        // only clients not host
        if(!isServer)
        {   
            clientLocalPlayerInputMsg = new PlayerInputMsg();
            clientLocalPlayerInputBuffer = new PlayerInput[PlayerStateBufferSize];
            clientLocalPlayerGameStateBuffer = new GameState[PlayerStateBufferSize];
            for(int i = 0;i<PlayerStateBufferSize;i++)
            {
                clientLocalPlayerInputBuffer[i] = new PlayerInput();
                clientLocalPlayerGameStateBuffer[i] = new GameState(myAnimator);
            }

            clientLocalPlayerReconciliationPhysicsFixedUpdateHandler = new CustomFixedUpdate(TickManger.PhysicsFixedDeltaTime,TickManger.PhysicsMaxAllowedTimestep,TickManger.PhysicsStep);

            TickManger.LocalPlayerInputCollectionEvent += LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent += LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent += LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent += LocalPlayerOtherGameStateUpdate;
            TickManger.StoreCurrentClientGameStateAndInputEvent += StoreCurrentClientGameStateAndInput;
            TickManger.clientMessageEvent += ClientMessage;
        }

        Camera.main.GetComponent<CameraController>().followTarget = gameObject.transform;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        //for client objects other than localplayer
        if(!isLocalPlayer)
        {   
            clientOtherPlayerGameStatesFromServerQueue = new Queue<GameState>();
            clientOtherPlayerGameStateReferencesForInterpolationQueue = new Queue<GameState>();
            clientOtherPlayerGameStateInterpolationQueue = new QueuedCoroutine(StartCoroutine,StopCoroutine);
            clientOtherPlayerGameStateInterpolationQueue.Start();
        }
    }

    public override void OnStartServer()
    {   
        base.OnStartServer(); 

        ServerGameStateList = new GameState[PlayerStateBufferSize];
        for(int i=0;i<PlayerStateBufferSize;i++)
        {
            ServerGameStateList[i] = new GameState(myAnimator);
        }

        ServerAllRigidBody2DList.Add(this.transform,this.myRigidbody2D);
        ServerAllRigidBodyList.Add(this.transform,this.myRigidbody);
        ServerAllAnimatorList.Add(this.transform,this.myAnimator);
        ServerAllCurrentGameStateList.Add(this.transform,this.myCurrentGameState);

        ServerAllGameStateList.Add(this.transform,this.ServerGameStateList);
        ServerApplyGameStateActionList.Add(this.transform,this.ApplyGameStateToLocalVariables);
        
        #if USING_PHYSICS2D
            myRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        #endif
        #if USING_PHYSICS3D
            myRigidbody.isKinematic = false;
        #endif
        
        ServerGameStateForclient = new GameState(myAnimator);
        
        TickManger.ServerGameStateStoreEvent += ServerStoreGameState;

        //only host
        if(isLocalPlayer)
        {
            TickManger.LocalPlayerInputCollectionEvent += LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent += LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent += LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent += LocalPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent +=  ServerMessage;
        }
        //only spawned objects by clients on server
        if(!isLocalPlayer)
        {  
            ServerMsgFromClient = new PlayerInputMsg();
            
            TickManger.PhysicsFixedUpdateEvent += ServerPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent += ServerPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent += ServerPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent +=  ServerMessage;
        }
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        ServerAllRigidBody2DList.Remove(this.transform);
        ServerAllRigidBodyList.Remove(this.transform);
        ServerAllAnimatorList.Remove(this.transform);
        ServerAllCurrentGameStateList.Remove(this.transform);

        ServerAllGameStateList.Remove(this.transform);
        ServerApplyGameStateActionList.Remove(this.transform);
    }

    private void OnEnable() 
    {   
        //only client not host
        if(isLocalPlayer&&!isServer)
        { 
            TickManger.LocalPlayerInputCollectionEvent -= LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent -= LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent -= LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent -= LocalPlayerOtherGameStateUpdate;
            TickManger.StoreCurrentClientGameStateAndInputEvent -= StoreCurrentClientGameStateAndInput;
            TickManger.clientMessageEvent -= ClientMessage;

            TickManger.LocalPlayerInputCollectionEvent += LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent += LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent += LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent += LocalPlayerOtherGameStateUpdate;
            TickManger.StoreCurrentClientGameStateAndInputEvent += StoreCurrentClientGameStateAndInput;
            TickManger.clientMessageEvent += ClientMessage;
        }
        //only host
        if(isServer&&isLocalPlayer)
        { 
            TickManger.LocalPlayerInputCollectionEvent -= LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent -= LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent -= LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent -= LocalPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent -=  ServerMessage;

            TickManger.LocalPlayerInputCollectionEvent += LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent += LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent += LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent += LocalPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent +=  ServerMessage;
        }
        //only server spawned objects
        if(isServer&&!isLocalPlayer)
        {
            TickManger.PhysicsFixedUpdateEvent -= ServerPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent -= ServerPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent -= ServerPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent -=  ServerMessage;

            TickManger.PhysicsFixedUpdateEvent += ServerPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent += ServerPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent += ServerPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent +=  ServerMessage;
        }
        if(isServer)
        {
            TickManger.ServerGameStateStoreEvent -= ServerStoreGameState;
            
            TickManger.ServerGameStateStoreEvent += ServerStoreGameState;
        }


        localPlayerInputManager.Enable();
    }

    private void OnDisable() 
    {         
        //only client not host
        if(isLocalPlayer&&!isServer)
        { 
            TickManger.LocalPlayerInputCollectionEvent -= LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent -= LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent -= LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent -= LocalPlayerOtherGameStateUpdate;
            TickManger.StoreCurrentClientGameStateAndInputEvent -= StoreCurrentClientGameStateAndInput;
        }
        //only host
        if(isServer&&isLocalPlayer)
        { 
            TickManger.LocalPlayerInputCollectionEvent -= LocalPlayerInputCollection;
            TickManger.PhysicsFixedUpdateEvent -= LocalPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent -= LocalPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent -= LocalPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent -=  ServerMessage;
        }
        //only server spawned objects
        if(isServer&&!isLocalPlayer)
        {                
            TickManger.PhysicsFixedUpdateEvent -= ServerPlayerPhysicsFixedUpdate;
            TickManger.AnimatorUpdateEvent -= ServerPlayerAnimationUpdate;
            TickManger.OtherGameStateUpdateEvent -= ServerPlayerOtherGameStateUpdate;
            TickManger.ServerMessageEvent -=  ServerMessage;
        }
        if(isServer)
        {
            TickManger.ServerGameStateStoreEvent -= ServerStoreGameState;
        }

        localPlayerInputManager.Disable();
    }



    #region Event Subscribers using functions defined 

    [Client]
    public void LocalPlayerInputCollection()
    {
        AssignLatestPlayerInput(localPlayerInput);
    }

    [Client]
    private void LocalPlayerPhysicsFixedUpdate()
    {
        PhysicsStateUpdate(localPlayerInput,false);
    }

    [Server]
    private void ServerPlayerPhysicsFixedUpdate()
    {
        PhysicsStateUpdate(ServerMsgFromClient.playerInput,false);
    }

    [Client]
    private void LocalPlayerAnimationUpdate()
    {
        AnimationStateUpdate(localPlayerInput,false);
    }

    [Server]
    private void ServerPlayerAnimationUpdate()
    {   
        AnimationStateUpdate(ServerMsgFromClient.playerInput,false);
    }

    [Client]
    private void LocalPlayerOtherGameStateUpdate()
    {
        OtherGameStateUpdate(localPlayerInput,false);
    }
    
    [Server]
    private void ServerPlayerOtherGameStateUpdate()
    {
        OtherGameStateUpdate(ServerMsgFromClient.playerInput,false);
    }
    
    [Client]
    private void StoreCurrentClientGameStateAndInput()
    {
        uint localPlayerbufferSlot = TickManger.tick % PlayerStateBufferSize;
        
        clientLocalPlayerInputBuffer[localPlayerbufferSlot] = localPlayerInput;
        //deep copy localPlayerInput to itself , to derefernce from stored states  
        CustomSerialization.Deserializer<PlayerInput>(CustomSerialization.Serializer<PlayerInput>(localPlayerInput) , localPlayerInput = new PlayerInput());

        AssignLatestPhysicsAndAnimatorStateToGameState(myCurrentGameState);
        myCurrentGameState.tick = TickManger.tick;
        clientLocalPlayerGameStateBuffer[localPlayerbufferSlot] = myCurrentGameState;

        //deep copy myCurrentGameState to itself , to derefernce from stored states  
        CustomSerialization.Deserializer<GameState>(CustomSerialization.Serializer<GameState>(myCurrentGameState) , myCurrentGameState = new GameState(myAnimator));
    }
    [Server]
    private void ServerStoreGameState()
    {
        uint localPlayerbufferSlot = TickManger.tick % PlayerStateBufferSize;

        AssignLatestPhysicsAndAnimatorStateToGameState(myCurrentGameState);
        myCurrentGameState.tick = TickManger.tick;
        ServerGameStateList[localPlayerbufferSlot] = myCurrentGameState;

        //deep copy myCurrentGameState to itself , to derefernce from stored states  
        CustomSerialization.Deserializer<GameState>(CustomSerialization.Serializer<GameState>(myCurrentGameState) , myCurrentGameState = new GameState(myAnimator));
    }

    [Server]
    public void ServerMessage()
    {   
        TargetServerReconciliation(CustomSerialization.Serializer<GameState>(myCurrentGameState));
        RpcEntityInterpolation(CustomSerialization.Serializer<GameState>(myCurrentGameState));
    }

    [Client]
    public void ClientMessage()
    {
        clientLocalPlayerInputMsg.tick = TickManger.tick;
        clientLocalPlayerInputMsg.playerInput = localPlayerInput;
        
        CmdsendLocalPlayerInputMsg(CustomSerialization.Serializer<PlayerInputMsg>(clientLocalPlayerInputMsg));  
        
        //host never updates its value
        if(!isServer)
        {
            CmdsendTicksToGoBackForLagCompensation(TickManger.TicksToGoBackForLagCompensation);
        }
    }

    #endregion

    #region NetworkCalls

    [Command]
    private void CmdsendLocalPlayerInputMsg(byte[] _playerInputMsgByte)
    {   
        CustomSerialization.Deserializer<PlayerInputMsg>(_playerInputMsgByte,ServerMsgFromClient);
    }
    
    [Command]
    private void CmdsendTicksToGoBackForLagCompensation(uint _TicksToGoBackForLagCompensation)
    {   
        TicksToGoBackForLagCompensationFromClient = _TicksToGoBackForLagCompensation;
    }

    [TargetRpc]
    private void TargetServerReconciliation(byte[] _GameStateByte)
    {   
        //server side reconciliation is never used by host
        if(isServer&&isLocalPlayer)
        {
            return;   
        }
        
        GameState clientLocalPlayerGameStateFromServer = new GameState(myAnimator);
        CustomSerialization.Deserializer<GameState>(_GameStateByte,clientLocalPlayerGameStateFromServer);

        if(clientLocalPlayerGameStateFromServer.tick<=clientLocalPlayerLastAcceptedState)
        {
            return;
        }
        else
        {
            clientLocalPlayerLastAcceptedState = clientLocalPlayerGameStateFromServer.tick;
        }
        
        uint bufferSlot = clientLocalPlayerGameStateFromServer.tick % PlayerStateBufferSize;

        if((!IsPlayerStateEqual(clientLocalPlayerGameStateFromServer,clientLocalPlayerGameStateBuffer[bufferSlot]))||clientLocalPlayerGameStateFromServer.tick!=clientLocalPlayerGameStateBuffer[bufferSlot].tick)
        {   

            myCurrentGameState = clientLocalPlayerGameStateFromServer;
            ApplyGameStateToLocalVariables(clientLocalPlayerGameStateFromServer);

            uint rewindTick = clientLocalPlayerGameStateFromServer.tick;
            
            if(rewindTick>=TickManger.tick)
            {
                return;
            }

            clientLocalPlayerReconciliationPhysicsFixedUpdateHandler.referenceTime = clientLocalPlayerGameStateFromServer.tick*TickManger.tickDelta;
            clientLocalPlayerReconciliationPhysicsFixedUpdateHandler.fixedTime = clientLocalPlayerReconciliationPhysicsFixedUpdateHandler.referenceTime - ( clientLocalPlayerReconciliationPhysicsFixedUpdateHandler.referenceTime%clientLocalPlayerReconciliationPhysicsFixedUpdateHandler.fixedDeltaTime);

            while(rewindTick<TickManger.tick)
            {   
                bufferSlot = rewindTick % PlayerStateBufferSize;
                
                clientLocalPlayerGameStateBuffer[bufferSlot] = myCurrentGameState;
                clientLocalPlayerGameStateBuffer[bufferSlot].tick = rewindTick;
                AssignLatestPhysicsAndAnimatorStateToGameState(clientLocalPlayerGameStateBuffer[bufferSlot]);

                //deep copy myCurrentGameState to itself , to de refernce from stored states  
                CustomSerialization.Deserializer<GameState>(CustomSerialization.Serializer<GameState>(myCurrentGameState) , myCurrentGameState = new GameState(myAnimator));

                OtherGameStateUpdate(clientLocalPlayerInputBuffer[bufferSlot],true);
                PhysicsStateUpdate(clientLocalPlayerInputBuffer[bufferSlot],true);
                AnimationStateUpdate(clientLocalPlayerInputBuffer[bufferSlot],true);

                clientLocalPlayerReconciliationPhysicsFixedUpdateHandler.Update(TickManger.tickDelta);
                myAnimator.Update((float)TickManger.tickDelta);
                
                rewindTick++;
            }

        }
    }


    [ClientRpc(includeOwner = false)]
    private void RpcEntityInterpolation(byte[] _GameStateByte)
    {   
        if(isServer||isLocalPlayer)// never ran for host or localplayer
        {
            return;
        }
        
        GameState clientOtherPlayerLatestGameStateFromServer = new GameState(myAnimator);

        CustomSerialization.Deserializer<GameState>(_GameStateByte,clientOtherPlayerLatestGameStateFromServer);

        if(clientOtherPlayerLatestGameStateFromServer.tick<=clientOtherPlayerLastAcceptedState)
        {
            return;
        }
        else
        {
            clientOtherPlayerLastAcceptedState = clientOtherPlayerLatestGameStateFromServer.tick;
        }

        if(TickManger.TicksToWaitForEntityInterpolation == 0)
        {
            myCurrentGameState = clientOtherPlayerLatestGameStateFromServer;
            ApplyGameStateToLocalVariables(myCurrentGameState);
            return;   
        }

        while(true)
        {
            if(clientOtherPlayerGameStatesFromServerQueue.Count==0)
            {
                break;
            }
        
            uint TickNeededByFirstElementForInterpolation = clientOtherPlayerGameStatesFromServerQueue.Peek().tick + TickManger.TicksToWaitForEntityInterpolation;

            if(clientOtherPlayerLatestGameStateFromServer.tick < TickNeededByFirstElementForInterpolation)
            {            
                //it is too soon to start Interpolation Coroutines
                break;
            }
            else if(clientOtherPlayerLatestGameStateFromServer.tick > TickNeededByFirstElementForInterpolation)
            {   
                //first element failed to find its interpolation partner and can't be used anymore
                //so we remove it and again check the next queue GameState w.r.t recieved GameState

                clientOtherPlayerGameStatesFromServerQueue.Dequeue();
                continue;
            }
            else //clientOtherPlayerGameStateFromServer.tick == TickNeededByFirstElementForInterpolation
            {   
                StartGameStateInterpolation();
                break;
            }
        }
        
        clientOtherPlayerGameStatesFromServerQueue.Enqueue(clientOtherPlayerLatestGameStateFromServer);

        //below is to interpolate in case we dont recieve another interpolation message within time

        if(clientOtherPlayerActiveCoroutineReference!=null)
        {
            StopCoroutine(clientOtherPlayerActiveCoroutineReference);
        }

        if(clientOtherPlayerGameStatesFromServerQueue.Count>1)
        {   
            float timeToWait = (float)((clientOtherPlayerGameStatesFromServerQueue.Peek().tick+TickManger.TicksToWaitForEntityInterpolation-clientOtherPlayerLatestGameStateFromServer.tick)*TickManger.tickDelta);
            clientOtherPlayerActiveCoroutineReference = StartCoroutine(StartGameStateInterpolationAfter(timeToWait));
        }
        else if(clientOtherPlayerGameStatesFromServerQueue.Count==1)
        {   
            clientOtherPlayerActiveCoroutineReference = StartCoroutine(ApplyGameStateAfter(TickManger.TicksToWaitForEntityInterpolation));
        }

        //end of function
        return;

        void StartGameStateInterpolation()
        {
            clientOtherPlayerGameStateInterpolationQueue.Start();
            clientOtherPlayerGameStateReferencesForInterpolationQueue.Clear();

            //transfer queue members and latest recived GameState to a separate queue 
            //as every parameter need to be alive foreach GameStateInterpolater in GameStateInterpolationQueue   
            while(clientOtherPlayerGameStatesFromServerQueue.Count!=0)
            {
                clientOtherPlayerGameStateReferencesForInterpolationQueue.Enqueue(clientOtherPlayerGameStatesFromServerQueue.Dequeue());
            }
            clientOtherPlayerGameStateReferencesForInterpolationQueue.Enqueue(clientOtherPlayerLatestGameStateFromServer);
            
            //queue in all relevant GameStateInterpolater coroutines to GameStateInterpolationQueue   
            while(clientOtherPlayerGameStateReferencesForInterpolationQueue.Count>2)
            {
                clientOtherPlayerGameStateInterpolationQueue.Enqueue(GameStateInterpolater(clientOtherPlayerGameStateReferencesForInterpolationQueue.Dequeue(),clientOtherPlayerGameStateReferencesForInterpolationQueue.Peek()));
            }
            if(clientOtherPlayerGameStateReferencesForInterpolationQueue.Count==2)
            {
                clientOtherPlayerGameStateInterpolationQueue.Enqueue(GameStateInterpolater(clientOtherPlayerGameStateReferencesForInterpolationQueue.Dequeue(),clientOtherPlayerGameStateReferencesForInterpolationQueue.Dequeue()));
            }
        }

        IEnumerator StartGameStateInterpolationAfter(float _timeToWait)
        {
            yield return new WaitForSecondsRealtime(_timeToWait);

            //almost same as StartGameStateInterpolation() but here clientOtherPlayerLatestGameStateFromServer is already queued in 
            //and at the end of coroutine we add it back to clientOtherPlayerGameStatesFromServerQueue

            clientOtherPlayerGameStateInterpolationQueue.Start();
            clientOtherPlayerGameStateReferencesForInterpolationQueue.Clear();

            while(clientOtherPlayerGameStatesFromServerQueue.Count!=0)
            {
                clientOtherPlayerGameStateReferencesForInterpolationQueue.Enqueue(clientOtherPlayerGameStatesFromServerQueue.Dequeue());
            }
            
            while(clientOtherPlayerGameStateReferencesForInterpolationQueue.Count>2)
            {
                clientOtherPlayerGameStateInterpolationQueue.Enqueue(GameStateInterpolater(clientOtherPlayerGameStateReferencesForInterpolationQueue.Dequeue(),clientOtherPlayerGameStateReferencesForInterpolationQueue.Peek()));
            }
            if(clientOtherPlayerGameStateReferencesForInterpolationQueue.Count==2)
            {
                clientOtherPlayerGameStateInterpolationQueue.Enqueue(GameStateInterpolater(clientOtherPlayerGameStateReferencesForInterpolationQueue.Dequeue(),clientOtherPlayerGameStateReferencesForInterpolationQueue.Dequeue()));
            }

            clientOtherPlayerGameStatesFromServerQueue.Enqueue(clientOtherPlayerLatestGameStateFromServer);

            yield break;
        }

        IEnumerator ApplyGameStateAfter(float timeToWait)
        {
            yield return new WaitForSecondsRealtime(timeToWait);
            myCurrentGameState = clientOtherPlayerLatestGameStateFromServer;
            ApplyGameStateToLocalVariables(myCurrentGameState);
            yield break;
        }
    }
    
    #endregion

    private IEnumerator GameStateInterpolater(GameState _startState,GameState _endState)
    {   
        myCurrentGameState = _startState;
        ApplyGameStateToLocalVariables(_startState);

        double startTime = NetworkTime.localTime;
        double duration = (_endState.tick-_startState.tick)*TickManger.tickDelta;
        double endTime = NetworkTime.localTime + duration;

        while(NetworkTime.localTime<=endTime)
        {   
            ApplyLinearlyInterpolatedGameState(_startState,_endState,(float)((NetworkTime.localTime-startTime)/duration));
            yield return null;
        }
        yield break;
    }

    [ServerCallback]
    private void LagCompensationRequest(Action _request)
    {   

        if(TicksToGoBackForLagCompensationFromClient == 0)
        {
            _request.Invoke();
        }
        else
        {
            Transform playerTransform;
            uint tickToSet;

            if(TickManger.isTickFrame)
            {
                tickToSet =TickManger.tick-1-TicksToGoBackForLagCompensationFromClient;
            }
            else
            {
                tickToSet =TickManger.tick-TicksToGoBackForLagCompensationFromClient;
            }


            foreach(var x in NetworkServer.connections.Values)
            {   
                playerTransform = x.identity.transform;
                
                if(playerTransform!=this.transform)
                {   
                    
                    ServerApplyGameStateActionList[playerTransform].Invoke(ServerAllGameStateList[playerTransform][tickToSet%PlayerStateBufferSize]);
                }
            }       
            
            _request.Invoke();

            if(TickManger.isTickFrame)
            {
                tickToSet =TickManger.tick-1;
            }
            else
            {
                tickToSet =TickManger.tick;
            }

            foreach(var x in NetworkServer.connections.Values)
            {   
                playerTransform = x.identity.transform;
                if(playerTransform!=this.transform)
                {   
                    ServerApplyGameStateActionList[playerTransform].Invoke(ServerAllGameStateList[playerTransform][tickToSet%PlayerStateBufferSize]);
                }
            }       
        }

    }

    #region NetworkedDataModels To Be Defined
        
    /// <summary>
    /// A class to store all inputs for Player.
    /// </summary>
    public class PlayerInput : INetworkedDataSerializable
    {   
        public Vector2 move;
        public bool fire;
        public Vector2 fireTarget;

        public PlayerInput()
        {

        }
        public void Serialize(BinaryWriter _writer)
        {
            _writer.Write(this.move.x);
            _writer.Write(this.move.y);
            _writer.Write(this.fire);
            _writer.Write(this.fireTarget.x);
            _writer.Write(this.fireTarget.y);
        }
        public void Deserialize(BinaryReader _reader)
        {
            this.move = new Vector2(_reader.ReadSingle(),_reader.ReadSingle()); 
            this.fire = _reader.ReadBoolean();
            this.fireTarget = new Vector2(_reader.ReadSingle(),_reader.ReadSingle());            
        }
    }

    /// <summary>
    /// A class that just has PlayerInput and tick no. just need to construct Player input 
    /// </summary>
    public class PlayerInputMsg : INetworkedDataSerializable 
    {
        public uint tick = 0;
        public PlayerInput playerInput;

        public PlayerInputMsg()
        {
            this.playerInput = new PlayerInput();
        }

        public void Serialize(BinaryWriter _writer)
        {   
            _writer.Write(this.tick);
            this.playerInput.Serialize(_writer);
        }

        public void Deserialize(BinaryReader _reader)
        {   
            this.tick =_reader.ReadUInt32();
            this.playerInput.Deserialize(_reader);
        } 
    }

    public class GameState : INetworkedDataSerializable
    {
        public uint tick = 0;
        public Vector3 position = Vector3.zero;
        public Vector3 velocity = Vector3.zero;
        public float timeWaitedToFire;
        public CustomAnimatorState animatorState;

        public GameState(Animator _animator)
        {
            this.animatorState = new CustomAnimatorState(_animator);
        }

        public void Serialize(BinaryWriter _writer)
        {   
            _writer.Write(this.position.x);
            _writer.Write(this.position.y);
            _writer.Write(this.velocity.x);
            _writer.Write(this.velocity.y);
            _writer.Write(this.timeWaitedToFire);
            this.animatorState.Serialize(_writer);
            _writer.Write(this.tick);
        }

        public void Deserialize(BinaryReader _reader)
        {   
            this.position =new Vector3(
                _reader.ReadSingle(),
                _reader.ReadSingle(),0f);
            this.velocity =new Vector3(
                _reader.ReadSingle(),
                _reader.ReadSingle(),0f);
            this.timeWaitedToFire = _reader.ReadSingle();
            this.animatorState.Deserialize(_reader); 
            this.tick =_reader.ReadUInt32();             
        } 
    }
    #endregion

    #region Functions to be defined as per NetworkedDataModels
        
    /// <summary>
    /// How other GameState should change before every Frame, according to given _playerInput and current Game State (myCurrentGameState)
    /// </summary>
    private void OtherGameStateUpdate(PlayerInput _playerInput,bool _IsBeingUsedDuringReconcilliation)
    {   
        if(!_IsBeingUsedDuringReconcilliation)
        {
           FireInServer(_playerInput);
        }
    }

    /// <summary>
    /// How Physics State should change before every PhysicsUpdate, according to given  _playerInput and current Physics State (myRigidboy)
    /// (like we do for physics inside fixed update of unity)
    /// </summary>
    private void PhysicsStateUpdate(PlayerInput _playerInput,bool _IsBeingUsedDuringReconcilliation)
    {
        myRigidbody2D.velocity = new Vector2(_playerInput.move.x*2.5f,myRigidbody2D.velocity.y);

        if(Physics2D.Raycast(transform.position,Vector2.down,mycollider.bounds.extents.y+0.2f,Ground))
        {   
            if(_playerInput.move.y>0.5f)
            myRigidbody2D.AddForce(Vector2.up*3f,ForceMode2D.Impulse);
        }

    }

    /// <summary>
    /// How Animator State should change before every AnimatorUpdate, according to given _playerInput and current Animator State (myAnimator)
    /// </summary>
    private void AnimationStateUpdate(PlayerInput _PlayerInput,bool _IsBeingUsedDuringReconcilliation)
    {
        if(_PlayerInput.move.sqrMagnitude>0)
        {
            myAnimator.SetBool("isMoving",true);
        }
        else
        {
            myAnimator.SetBool("isMoving",false);
        }
    }

    
    /// <summary>
    /// To assign latest PlayerInput to _assignMe
    /// </summary>
    private void AssignLatestPlayerInput(PlayerInput _assignMe)
    {
        _assignMe.move = localPlayerInputManager.Character.move.ReadValue<Vector2>();
        _assignMe.fire = localPlayerInputManager.Character.fire.ReadValue<float>()>0.5f;
        _assignMe.fireTarget =  Camera.main.ScreenToWorldPoint(localPlayerInputManager.Character.pointerPosition.ReadValue<Vector2>());
    }

    /// <summary>
    /// To assign latest Physics State(myRigidBody) and Animator State(myAnimator) values to _assignMe
    /// </summary>
    private void AssignLatestPhysicsAndAnimatorStateToGameState(GameState _assignMe)
    {
        _assignMe.position = myRigidbody2D.position;
        _assignMe.velocity = myRigidbody2D.velocity;
        _assignMe.animatorState.SetThisObject(myAnimator);
    }

    /// <summary>
    /// To change Physics State(myRigidBody) and Animator State(myAnimator) values as per _applyMe
    /// </summary>
    private void ApplyGameStateToLocalVariables(GameState _applyMe)
    {
        myRigidbody2D.position = _applyMe.position;
        myRigidbody2D.velocity = _applyMe.velocity;
        _applyMe.animatorState.syncAnimatorState(myAnimator);
    }

    /// <summary>
    /// Returns if _playerState1 matches with _playerState2  
    /// </summary>
    private bool IsPlayerStateEqual(GameState _playerState1,GameState _playerState2)
    {
        if(_playerState1.Equals(_playerState2))
        return true;

        if(Mathf.Abs(Vector3.Distance(_playerState1.position,_playerState2.position))>0.01f)
        {   
            return false;
        }
        if(Mathf.Abs(Vector3.Distance(_playerState1.velocity,_playerState2.velocity))>0.01f)
        {   
            return false;
        }    

        return true;
    }

    /// <summary>
    /// Apply a linearlyInterpolated PlayerState Between _startState and _endState based on _interpolationValue which belongs to [0,1]
    /// (note) CustomAnimatorState, if used, should not be interpolated , as Mechanim will itself play the animations from startState  
    /// </summary>
    public void ApplyLinearlyInterpolatedGameState(GameState _startState,GameState _endState,float _interpolationValue)
    {
        myRigidbody2D.position = Vector3.Lerp(_startState.position,_endState.position,_interpolationValue);
        myRigidbody2D.velocity = Vector3.Lerp(_startState.velocity,_endState.velocity,_interpolationValue);
    }

    #endregion

    void FireInServer(PlayerInput _playerInput)
    {
        if(myCurrentGameState.timeWaitedToFire<0.5f)
        {
            myCurrentGameState.timeWaitedToFire+=Time.deltaTime;
            return;
        }
        if(_playerInput.fire)
        {   
            myCurrentGameState.timeWaitedToFire = 0f;
            _target =_playerInput.fireTarget;
            LagCompensationRequest(checkIfPlayerHit);
        }
    }
    
    
    //Lag Compensated Action
    Vector2 _target;
    void checkIfPlayerHit()
    {
        Vector2 myPos = this.transform.position;
        RaycastHit2D hit =  Physics2D.Raycast(myPos,_target-myPos,(_target-myPos).magnitude,Player);
        if(hit.transform!=null)
        {
            TargetDrawRay(myPos,hit.point,true);
        }
        else
        {
            TargetDrawRay(myPos,_target,false);
        }
        
    }

    [TargetRpc]
    private void TargetDrawRay(Vector2 _startPoint,Vector2 _endPoint,bool isHit)
    {
        DrawRayCast spawnedObjectScript = Instantiate(DrawRaycastPrefab,Vector3.zero,Quaternion.identity).GetComponent<DrawRayCast>();
        spawnedObjectScript.myLineRenderer.SetPositions(new Vector3[]{_startPoint,_endPoint});
        if(isHit)
        {
            spawnedObjectScript.myLineRenderer.startColor = Color.green;
            spawnedObjectScript.myLineRenderer.endColor = Color.green; 
        }
        else
        {
            spawnedObjectScript.myLineRenderer.startColor = Color.red;
            spawnedObjectScript.myLineRenderer.endColor = Color.red; 
        }
    }

}