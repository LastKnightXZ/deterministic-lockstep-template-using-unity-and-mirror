//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.2.0
//     from Assets/DeterministicLocstepTemplate/Demo/Scripts/InputManager.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @InputManager : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputManager()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputManager"",
    ""maps"": [
        {
            ""name"": ""Character"",
            ""id"": ""ff9a1006-986b-46c0-9960-d4a5898a0682"",
            ""actions"": [
                {
                    ""name"": ""move"",
                    ""type"": ""Value"",
                    ""id"": ""658f338f-651a-4b06-aaf7-5572e1f3e034"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""fire"",
                    ""type"": ""Button"",
                    ""id"": ""72ef8edd-0ac3-44a6-a1d4-97b63a21013a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""pointerPosition"",
                    ""type"": ""Value"",
                    ""id"": ""d4392f57-e7cb-4176-b407-72681daf46d9"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""2272aa42-6075-42b2-8d11-788aced65ced"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""b3a74ef5-0ac1-4304-a1a9-b8e07238e32b"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d640984a-51da-4032-bbd9-9d2ec9411e11"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""399a1e97-ef8e-46a2-a981-dec416d21865"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""f862fe20-97da-4b92-9f5d-d9b1c048042c"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""07319f1a-10c4-4fca-acfb-8040f5f35c78"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""833eb1f3-5d6a-491e-8358-3a2366821dc5"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""11fe826d-344e-4b12-91e8-58c85d0aaf7c"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""pointerPosition"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Character
        m_Character = asset.FindActionMap("Character", throwIfNotFound: true);
        m_Character_move = m_Character.FindAction("move", throwIfNotFound: true);
        m_Character_fire = m_Character.FindAction("fire", throwIfNotFound: true);
        m_Character_pointerPosition = m_Character.FindAction("pointerPosition", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Character
    private readonly InputActionMap m_Character;
    private ICharacterActions m_CharacterActionsCallbackInterface;
    private readonly InputAction m_Character_move;
    private readonly InputAction m_Character_fire;
    private readonly InputAction m_Character_pointerPosition;
    public struct CharacterActions
    {
        private @InputManager m_Wrapper;
        public CharacterActions(@InputManager wrapper) { m_Wrapper = wrapper; }
        public InputAction @move => m_Wrapper.m_Character_move;
        public InputAction @fire => m_Wrapper.m_Character_fire;
        public InputAction @pointerPosition => m_Wrapper.m_Character_pointerPosition;
        public InputActionMap Get() { return m_Wrapper.m_Character; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CharacterActions set) { return set.Get(); }
        public void SetCallbacks(ICharacterActions instance)
        {
            if (m_Wrapper.m_CharacterActionsCallbackInterface != null)
            {
                @move.started -= m_Wrapper.m_CharacterActionsCallbackInterface.OnMove;
                @move.performed -= m_Wrapper.m_CharacterActionsCallbackInterface.OnMove;
                @move.canceled -= m_Wrapper.m_CharacterActionsCallbackInterface.OnMove;
                @fire.started -= m_Wrapper.m_CharacterActionsCallbackInterface.OnFire;
                @fire.performed -= m_Wrapper.m_CharacterActionsCallbackInterface.OnFire;
                @fire.canceled -= m_Wrapper.m_CharacterActionsCallbackInterface.OnFire;
                @pointerPosition.started -= m_Wrapper.m_CharacterActionsCallbackInterface.OnPointerPosition;
                @pointerPosition.performed -= m_Wrapper.m_CharacterActionsCallbackInterface.OnPointerPosition;
                @pointerPosition.canceled -= m_Wrapper.m_CharacterActionsCallbackInterface.OnPointerPosition;
            }
            m_Wrapper.m_CharacterActionsCallbackInterface = instance;
            if (instance != null)
            {
                @move.started += instance.OnMove;
                @move.performed += instance.OnMove;
                @move.canceled += instance.OnMove;
                @fire.started += instance.OnFire;
                @fire.performed += instance.OnFire;
                @fire.canceled += instance.OnFire;
                @pointerPosition.started += instance.OnPointerPosition;
                @pointerPosition.performed += instance.OnPointerPosition;
                @pointerPosition.canceled += instance.OnPointerPosition;
            }
        }
    }
    public CharacterActions @Character => new CharacterActions(this);
    public interface ICharacterActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnFire(InputAction.CallbackContext context);
        void OnPointerPosition(InputAction.CallbackContext context);
    }
}