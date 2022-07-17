namespace LastKnightXZ.Utils.CustomFixedUpdate
{   
    public class CustomFixedUpdate
    {
        public double fixedDeltaTime{get; set;}
        public double referenceTime{get; set;}
        public double fixedTime{get; set;}
        public double maxAllowedTimestep{get; set;}
        public System.Action fixedUpdate; 
        private System.Diagnostics.Stopwatch timeout = new System.Diagnostics.Stopwatch();
    
        public CustomFixedUpdate(float _fixedDeltaTime,float _maxAllowedTimestep, System.Action _fixedUpdateCallback)
        {
            this.fixedDeltaTime = _fixedDeltaTime;
            this.referenceTime = 0f;
            this.fixedTime = 0f;
            this.maxAllowedTimestep = _maxAllowedTimestep;
            this.fixedUpdate = _fixedUpdateCallback;
        }
    
        public bool Update(double _deltaTime)
        {
            timeout.Reset();
            timeout.Start();
    
            referenceTime += _deltaTime;

            while ((referenceTime-fixedTime)>=fixedDeltaTime)
            {
                fixedTime += fixedDeltaTime;
                if (fixedUpdate != null)
                    fixedUpdate();
                if ((timeout.ElapsedMilliseconds / 1000.0d) > maxAllowedTimestep)
                    return false;
            }
            return true;
        }
        
    }
}

namespace LastKnightXZ.Utils.QueuedCoroutine
{   
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
        
    public class QueuedCoroutine 
    {
        private Queue<IEnumerator> Queuedcoroutine = new Queue<IEnumerator>();
        private IEnumerator masterCoordinatorReference = null;
        private bool isQueuedCoroutineRunning = false;

        public delegate Coroutine StartCoroutineDelegate(IEnumerator method);
        private StartCoroutineDelegate StartCoroutineMethod;
        public delegate void StopCoroutineDelegate(IEnumerator method);
        private StopCoroutineDelegate StopCoroutineMethod;

        public QueuedCoroutine(StartCoroutineDelegate _startCoroutineMethodOfTheMonoBehaviour,StopCoroutineDelegate _stopCoroutineMethodOfTheMonoBehaviour)
        {
            this.StartCoroutineMethod = _startCoroutineMethodOfTheMonoBehaviour;
            this.StopCoroutineMethod = _stopCoroutineMethodOfTheMonoBehaviour;
            masterCoordinatorReference = MasterCoroutine();
        }

        public void Start()
        {   
            if(!isQueuedCoroutineRunning)
            {   
                StartCoroutineMethod(masterCoordinatorReference);
            }
            else
            {
                Stop();
                StartCoroutineMethod(masterCoordinatorReference);                
            }

            isQueuedCoroutineRunning = true;
        }

        public void Stop()
        {
            Queuedcoroutine.Clear();
            StopCoroutineMethod(masterCoordinatorReference);
            isQueuedCoroutineRunning = false;
        }

        public void Enqueue(IEnumerator _coroutine)
        {
            Queuedcoroutine.Enqueue(_coroutine);
        }
        
        IEnumerator MasterCoroutine()
        {
            while (true)
            {
                while (Queuedcoroutine.Count > 0)
                {   
                    yield return Queuedcoroutine.Dequeue();
                }
                yield return null;
            }
        }        
    }
}