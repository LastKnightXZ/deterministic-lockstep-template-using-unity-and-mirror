using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;

namespace LastKnightXZ.NetworkedDataDefinations
{   

    /// <summary>
    /// an interface used for object thats going to be networked and be used by CustomSerialization
    /// </summary>
    public interface INetworkedDataSerializable
    {
        public void Serialize(BinaryWriter _writer);
        public void Deserialize(BinaryReader _reader);
    }
    
    /// <summary>
    /// usable only for classes under contract with INetworkedDataModel interface
    /// </summary>
    public static class CustomSerialization
    {
        public static byte[] Serializer<T>(T _objectToBeSerialized) where T : INetworkedDataSerializable
        {
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(BinaryWriter binaryWriter = new BinaryWriter(memoryStream,Encoding.UTF8, true))
                {
                    _objectToBeSerialized.Serialize(binaryWriter);
                    return memoryStream.ToArray();
                }
            }
        }
        public static void Deserializer<T>(byte[] _array,T _objectToHoldDeserialisedData) where T : INetworkedDataSerializable
        {
            using(MemoryStream memoryStream = new MemoryStream(_array))
            {
                using(BinaryReader binaryReader = new BinaryReader(memoryStream,Encoding.UTF8, true))
                {   
                    _objectToHoldDeserialisedData.Deserialize(binaryReader);
                }
            }        
        }
    } 

    #region DefinedNetworkedDataModels

    /// <summary>
    /// needs to be constructed by a animator instance with a controller in it,
    /// new state does not depend on previous states 
    /// </summary>
    public class CustomAnimatorParametersState : INetworkedDataSerializable
    {
        public RuntimeAnimatorController referenceController{get;private set;}
        public object[] parameters{get;private set;}
        
        public CustomAnimatorParametersState(Animator _AnimatorToCopy)
        {   
            this.referenceController =_AnimatorToCopy.runtimeAnimatorController;
                    
            AnimatorControllerParameter[] refParameters = _AnimatorToCopy.parameters
            .Where(par => !_AnimatorToCopy.IsParameterControlledByCurve(par.nameHash))
            .ToArray();

            this.parameters = new object[refParameters.Length];  

            for(int i=0;i<refParameters.Length;i++)
            {   
                AnimatorControllerParameter referenceParameter = refParameters[i];
                
                if(referenceParameter.type==AnimatorControllerParameterType.Int)
                {
                    this.parameters[i] = new int();
                    this.parameters[i] = _AnimatorToCopy.GetInteger(referenceParameter.name);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Float)
                {
                    this.parameters[i] = new float();
                    this.parameters[i] = _AnimatorToCopy.GetFloat(referenceParameter.name);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Bool)
                {
                    this.parameters[i] = new bool();
                    this.parameters[i] = _AnimatorToCopy.GetBool(referenceParameter.name);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Trigger)
                {
                    this.parameters[i] = new bool();
                    this.parameters[i] = _AnimatorToCopy.GetBool(referenceParameter.name);
                    continue;
                }             
            }                
        }
        public void SetThisObject(Animator _AnimatorToCopy)
        {   
            if(!this.referenceController.Equals(_AnimatorToCopy.runtimeAnimatorController))
            {
                Debug.LogError("Does not share AnimatorController");
                return;
            }
            
            AnimatorControllerParameter[] refParameters = _AnimatorToCopy.parameters
            .Where(par => !_AnimatorToCopy.IsParameterControlledByCurve(par.nameHash))
            .ToArray();

            for(int i=0;i<refParameters.Length;i++)
            {  
                AnimatorControllerParameter referenceParameter = refParameters[i];
                
                if(referenceParameter.type==AnimatorControllerParameterType.Int)
                {
                    this.parameters[i] = _AnimatorToCopy.GetInteger(referenceParameter.name);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Float)
                {
                    this.parameters[i] = _AnimatorToCopy.GetFloat(referenceParameter.name);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Bool)
                {
                    this.parameters[i] = _AnimatorToCopy.GetBool(referenceParameter.name);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Trigger)
                {
                    this.parameters[i] = _AnimatorToCopy.GetBool(referenceParameter.name);
                    continue;
                }             
            }                
        }   
        public void syncAnimatorParametersState(Animator _AnimatorToSync)
        {   
            if(!this.referenceController.Equals(_AnimatorToSync.runtimeAnimatorController))
            {
                Debug.LogError("Does not share AnimatorController");
                return;
            }

            AnimatorControllerParameter[] refParameters = _AnimatorToSync.parameters
            .Where(par => !_AnimatorToSync.IsParameterControlledByCurve(par.nameHash))
            .ToArray();

            for(int i=0;i<refParameters.Length;i++)
            {  
                AnimatorControllerParameter referenceParameter = refParameters[i];

                if(referenceParameter.type==AnimatorControllerParameterType.Int)
                {
                    _AnimatorToSync.SetInteger(referenceParameter.name,(int)this.parameters[i]);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Float)
                {
                    _AnimatorToSync.SetFloat(referenceParameter.name,(float)this.parameters[i]);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Bool)
                {
                    _AnimatorToSync.SetBool(referenceParameter.name,(bool)this.parameters[i]);
                    continue;
                }
                if(referenceParameter.type==AnimatorControllerParameterType.Trigger)
                {
                    if((bool)this.parameters[i])
                    {
                        _AnimatorToSync.SetTrigger(referenceParameter.name);
                    }
                    else
                    {
                        _AnimatorToSync.ResetTrigger(referenceParameter.name);
                    }
                    continue;
                }        
            }
            _AnimatorToSync.Update(0f); //apply changes by parameters
        }

        public bool IsEqual(CustomAnimatorParametersState _ObjectToCheck,float _MaxAllowedDifferenceForFloat)
        {
            if(!this.referenceController.Equals(_ObjectToCheck.referenceController))
            {
                Debug.LogError("Does not share AnimatorController");
                return false;
            }

            if(this.Equals(_ObjectToCheck))
            {
                return true;
            }

            for(int i=0;i<this.parameters.Length;i++)
            {
                if(this.parameters[i].GetType()==typeof(int))
                {
                    if((int)this.parameters[i] != (int)_ObjectToCheck.parameters[i])
                    return false;
                }
                if(this.parameters[i].GetType()==typeof(bool))
                {
                    if((bool)this.parameters[i] != (bool)_ObjectToCheck.parameters[i])
                    return false;
                }
                if(this.parameters[i].GetType()==typeof(float))
                {
                    if(Mathf.Abs((float)this.parameters[i]-(float)_ObjectToCheck.parameters[i])>_MaxAllowedDifferenceForFloat)
                    return false;
                }              
            }
            
            return true;
        }
        public void Serialize(BinaryWriter _writer) 
        {
            for(int i=0;i<this.parameters.Length;i++)
            {
                if(this.parameters[i].GetType()==typeof(int))
                {
                    _writer.Write((int)this.parameters[i]);
                    continue;
                }
                if(this.parameters[i].GetType()==typeof(float))
                {
                    _writer.Write((float)this.parameters[i]);
                    continue;
                }
                if(this.parameters[i].GetType()==typeof(bool))
                {
                    _writer.Write((bool)this.parameters[i]);
                    continue;
                }
            }
        }
        public void Deserialize(BinaryReader _reader)
        {
            for(int i=0;i<this.parameters.Length;i++)
            {
                if(this.parameters[i].GetType()==typeof(int))
                {
                    this.parameters[i]=_reader.ReadInt32(); 
                    continue;
                }
                if(this.parameters[i].GetType()==typeof(float))
                {
                    this.parameters[i]=_reader.ReadSingle();
                    continue;
                }
                if(this.parameters[i].GetType()==typeof(bool))
                {
                    this.parameters[i]=_reader.ReadBoolean();
                    continue;
                }
            }
        } 
    }

    /// <summary>
    /// new state depends on previous state , so setting of this class should be done every update frame
    /// </summary>
    public class CustomAnimatorLayerTransitionState : INetworkedDataSerializable
    {   
        public int transitionCount;
        public List<int> nextStateHash{get;private set;}
        public List<float> transitionDuration{get;private set;}
        public List<float> nextStateNormalisedTime{get;private set;}
        public List<float> transitionNormalisedTime{get;private set;}

        public CustomAnimatorLayerTransitionState()
        {
            this.transitionCount = 0;
            this.nextStateHash = new List<int>();
            this.transitionDuration = new List<float>();
            this.nextStateNormalisedTime = new List<float>();
            this.transitionNormalisedTime = new List<float>();
        }
        public void Reset()
        {   
            this.transitionCount = 0;
            this.nextStateHash.Clear();
            this.transitionDuration.Clear();
            this.nextStateNormalisedTime.Clear();
            this.transitionNormalisedTime.Clear();
        }
        public void SetThisObject(Animator _AnimatorToCopy,int _Layer)
        {
            if(!_AnimatorToCopy.IsInTransition(_Layer))
            {   
                if(this.transitionCount>0)
                {
                    Reset();
                }
                return;
            }
            else
            {   
                AnimatorTransitionInfo transitionStateInfo = _AnimatorToCopy.GetAnimatorTransitionInfo(_Layer);
                AnimatorStateInfo nextStateinfo =  _AnimatorToCopy.GetNextAnimatorStateInfo(_Layer);
                if(transitionCount == 0)
                {   
                    this.transitionCount ++;
                    this.nextStateHash.Add(nextStateinfo.fullPathHash);                 
                    this.transitionDuration.Add(transitionStateInfo.duration);
                    this.nextStateNormalisedTime.Add(nextStateinfo.normalizedTime);  
                    this.transitionNormalisedTime.Add(transitionStateInfo.normalizedTime);
                    return;   
                }
                if((transitionCount > 0)&&(this.nextStateHash[transitionCount-1]==nextStateinfo.fullPathHash))
                {               
                    this.transitionDuration[transitionCount-1] = transitionStateInfo.duration;
                    this.nextStateNormalisedTime[transitionCount-1] = nextStateinfo.normalizedTime;  
                    this.transitionNormalisedTime[transitionCount-1] = transitionStateInfo.normalizedTime;
                    return;
                }
                if((transitionCount > 0)&&(this.nextStateHash[transitionCount-1]!=nextStateinfo.fullPathHash))
                {
                    this.transitionCount ++;
                    this.nextStateHash.Add(nextStateinfo.fullPathHash);                 
                    this.transitionDuration.Add(transitionStateInfo.duration);
                    this.nextStateNormalisedTime.Add(nextStateinfo.normalizedTime);  
                    this.transitionNormalisedTime.Add(transitionStateInfo.normalizedTime);
                    return;   
                }
            }
        }
        public void SyncAnimatorLayerTransitionState(Animator _AnimatorToSync,int _Layer)
        {   
            if(this.transitionCount==0)
            {
                return;
            }
            //play chain of crossfade to replicate transtion interrupt
            for(int i =0;i<this.transitionCount;i++)
            {
                _AnimatorToSync.CrossFade(this.nextStateHash[i],this.transitionDuration[i],_Layer,this.nextStateNormalisedTime[i],this.transitionNormalisedTime[i]);
                _AnimatorToSync.Update(0f);
            }
        }
        public bool IsEqual(CustomAnimatorLayerTransitionState _ObjectToCheck,float _MaxAllowedDifferenceForFloat)
        {
            if(this.Equals(_ObjectToCheck))
            {
                return true;
            }
            
            if(this.transitionCount!=_ObjectToCheck.transitionCount)
            {
                return false;
            }

            for(int i = 0;i<this.transitionCount;i++)
            {
                if(this.nextStateHash[i]!=_ObjectToCheck.nextStateHash[i])
                {
                    return false;
                }
                if(this.transitionDuration[i]!=_ObjectToCheck.transitionDuration[i])
                {
                    return false;
                }
                if(Mathf.Abs(this.nextStateNormalisedTime[i]-_ObjectToCheck.nextStateNormalisedTime[i])>_MaxAllowedDifferenceForFloat)
                {
                    return false;
                }
                if(Mathf.Abs(this.transitionNormalisedTime[i]-_ObjectToCheck.transitionNormalisedTime[i])>_MaxAllowedDifferenceForFloat)
                {
                    return false;
                }                 
            }
            return true;
        }
        public void Serialize(BinaryWriter _writer)
        {
            _writer.Write(this.transitionCount);
            for(int i=0;i<this.transitionCount;i++)
            {
                _writer.Write(this.transitionDuration[i]);
                _writer.Write(this.transitionNormalisedTime[i]);
                _writer.Write(this.nextStateHash[i]);
                _writer.Write(this.nextStateNormalisedTime[i]);
            }          
        }
        public void Deserialize(BinaryReader _reader)
        {   
            this.transitionCount=_reader.ReadInt32();

            this.nextStateHash.Clear();
            this.transitionDuration.Clear();
            this.nextStateNormalisedTime.Clear();
            this.transitionNormalisedTime.Clear();

            for(int i =0;i<this.transitionCount;i++)
            {
                this.transitionDuration.Add(_reader.ReadSingle());
                this.transitionNormalisedTime.Add(_reader.ReadSingle());
                this.nextStateHash.Add(_reader.ReadInt32());
                this.nextStateNormalisedTime.Add(_reader.ReadSingle());
            }  
        }        
    }

    /// <summary>
    /// provides functionality to store and syncs animator's parameters and animator's animator State, transitions and layer weight of all layers,
    /// needs to be constructed by a animator instance with a controller in it.
    ///
    /// (note) most of the time parameters will itself take care of synchronization ,
    /// in case parameters are unable sync the animator
    /// then this class can synchronize state animation and uninterupted state transition 
    /// and interupted state transitions with sometimes one frame delay(work in progress)
    public class CustomAnimatorState : INetworkedDataSerializable
    {
        public CustomAnimatorParametersState animatorParametersHandler{get;private set;}
        public int[] currentStateHash{get;private set;}
        public float[] currentStateNormalisedTime{get;private set;}
        public CustomAnimatorLayerTransitionState[] animatortransitionsHandler{get;private set;}
        public float[] layerWeight{get;private set;}
                    
        public CustomAnimatorState(Animator _AnimatorToCopy)
        {
            this.animatorParametersHandler = new CustomAnimatorParametersState(_AnimatorToCopy);
            
            this.currentStateHash = new int[_AnimatorToCopy.layerCount];
            this.currentStateNormalisedTime = new float[_AnimatorToCopy.layerCount];
            
            this.animatortransitionsHandler = new CustomAnimatorLayerTransitionState[_AnimatorToCopy.layerCount];
            for(int i=0;i<_AnimatorToCopy.layerCount;i++)
            {
                this.animatortransitionsHandler[i] = new CustomAnimatorLayerTransitionState();
            }
            
            this.layerWeight = new float[_AnimatorToCopy.layerCount];

            for(int i=0;i<_AnimatorToCopy.layerCount;i++)
            {
                AnimatorStateInfo currentStateinfo =  _AnimatorToCopy.GetCurrentAnimatorStateInfo(i);
                this.currentStateHash[i] = currentStateinfo.fullPathHash;
                this.currentStateNormalisedTime[i] =  currentStateinfo.normalizedTime;
                
                this.animatortransitionsHandler[i].SetThisObject(_AnimatorToCopy,i);

                this.layerWeight[i] = _AnimatorToCopy.GetLayerWeight(i);
            }
        
        }
        public void SetThisObject(Animator _AnimatorToCopy)
        {
            this.animatorParametersHandler.SetThisObject(_AnimatorToCopy);

            for(int i=0;i<_AnimatorToCopy.layerCount;i++)
            {
                AnimatorStateInfo currentStateinfo =  _AnimatorToCopy.GetCurrentAnimatorStateInfo(i);
                this.currentStateHash[i] = currentStateinfo.fullPathHash;
                this.currentStateNormalisedTime[i] =  currentStateinfo.normalizedTime;
                
                //current state depends on previous state
                this.animatortransitionsHandler[i].SetThisObject(_AnimatorToCopy,i);

                this.layerWeight[i] = _AnimatorToCopy.GetLayerWeight(i);
            }
            
        }        
        public void syncAnimatorState(Animator _AnimatorToSync,float _MaxAllowedDifferenceForNormalisedTimes = 0.000001f)
        {   
            
            this.animatorParametersHandler.syncAnimatorParametersState(_AnimatorToSync);

            for(int i=0;i<_AnimatorToSync.layerCount;i++)
            {
                _AnimatorToSync.SetLayerWeight(i,this.layerWeight[i]);

                AnimatorStateInfo _CurrentAnimatorState = _AnimatorToSync.GetCurrentAnimatorStateInfo(i);  

                if((this.animatortransitionsHandler[i].transitionCount==0)&&
                ((_CurrentAnimatorState.fullPathHash!=this.currentStateHash[i])||(Mathf.Abs(_CurrentAnimatorState.normalizedTime-this.currentStateNormalisedTime[i])>_MaxAllowedDifferenceForNormalisedTimes)
                ))
                {   
                    _AnimatorToSync.Play(this.currentStateHash[i],i,this.currentStateNormalisedTime[i]);
                    _AnimatorToSync.Update(0f); //causes play to start before internal animation update                   
                }
                if(this.animatortransitionsHandler[i].transitionCount>0)
                {
                    _AnimatorToSync.Play(this.currentStateHash[i],0,this.currentStateNormalisedTime[i]);
                    _AnimatorToSync.Update(0f);//force play starting state before internal animation update
                    this.animatortransitionsHandler[i].SyncAnimatorLayerTransitionState(_AnimatorToSync,i);
                }
            
            }    
        
        }

        public bool IsEqual(CustomAnimatorState _ObjectToCheck,float _MaxAllowedDifferenceForFloat)
        {
            if(this.Equals(_ObjectToCheck))
            return true;

            if(!this.animatorParametersHandler.IsEqual(_ObjectToCheck.animatorParametersHandler,_MaxAllowedDifferenceForFloat))
            return false;

            if(this.currentStateHash.Length!=_ObjectToCheck.currentStateHash.Length)
            return false; 

            for(int i=0;i<this.currentStateHash.Length;i++)
            {   
                if(this.currentStateHash[i]!=_ObjectToCheck.currentStateHash[i])
                {
                    return false;
                }
                if(Mathf.Abs(this.currentStateNormalisedTime[i]-_ObjectToCheck.currentStateNormalisedTime[i])>_MaxAllowedDifferenceForFloat)
                {
                    return false;
                }
                if(!this.animatortransitionsHandler[i].IsEqual(_ObjectToCheck.animatortransitionsHandler[i],_MaxAllowedDifferenceForFloat))
                {   
                    return false;
                }
                if(Mathf.Abs(this.layerWeight[i]-_ObjectToCheck.layerWeight[i])>_MaxAllowedDifferenceForFloat)
                {
                    return false;
                }
            }

            return true;
        }

        public void Serialize(BinaryWriter _writer)
        {
            this.animatorParametersHandler.Serialize(_writer);

            for(int i=0;i<this.currentStateHash.Length;i++)
            {
                _writer.Write(this.currentStateHash[i]);
                _writer.Write(this.currentStateNormalisedTime[i]);
                this.animatortransitionsHandler[i].Serialize(_writer);
                _writer.Write(this.layerWeight[i]);
            }            
        }

        public void Deserialize(BinaryReader _reader)
        {    
            this.animatorParametersHandler.Deserialize(_reader);

            for(int i=0;i<this.currentStateHash.Length;i++)
            {
                this.currentStateHash[i]=_reader.ReadInt32();
                this.currentStateNormalisedTime[i]=_reader.ReadSingle();
                this.animatortransitionsHandler[i].Deserialize(_reader);
                this.layerWeight[i]=_reader.ReadSingle();
            }  
        }
    }
    #endregion
    
}    