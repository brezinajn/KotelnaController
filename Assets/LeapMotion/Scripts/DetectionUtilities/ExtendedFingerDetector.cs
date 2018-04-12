using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {

    /**
     * Detects when specified fingers are in an extended or non-extended state.
     * 
     * You can specify whether each finger is extended, not extended, or in either state.
     * This detector activates when every finger on the observed hand meets these conditions.
     * 
     * If added to a IHandModel instance or one of its children, this detector checks the
     * finger state at the interval specified by the Period variable. You can also specify
     * which hand model to observe explicitly by setting handModel in the Unity editor or 
     * in code.
     * 
     * @since 4.1.2
     */
    public class ExtendedFingerDetector : Detector {
        /**
         * The interval at which to check finger state.
         * @since 4.1.2
         */
        [Tooltip("The interval in seconds at which to check this detector's conditions.")]
        public float Period = .1f; //seconds

        /**
         * The IHandModel instance to observe. 
         * Set automatically if not explicitly set in the editor.
         * @since 4.1.2
         */
        [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
        public IHandModel HandModel = null;

        /** The required thumb state. */
        private PointingState Thumb = PointingState.Either;
        /** The required index finger state. */
        private PointingState Index = PointingState.Either;
        /** The required middle finger state. */
        private PointingState Middle = PointingState.Either;
        /** The required ring finger state. */
        private PointingState Ring = PointingState.Either;
        /** The required pinky finger state. */
        private PointingState Pinky = PointingState.Either;

        private int level = -1;
        private PointingState[][] gestures = new PointingState[3][] {
            new PointingState[5] {
                PointingState.NotExtended,
                PointingState.Extended,
                PointingState.NotExtended,
                PointingState.NotExtended,
                PointingState.NotExtended,
            },
            new PointingState[5] {
                PointingState.NotExtended,
                PointingState.Extended,
                PointingState.Extended,
                PointingState.Extended,
                PointingState.Extended,
            },
            new PointingState[5] {
                PointingState.NotExtended,
                PointingState.Extended,
                PointingState.NotExtended,
                PointingState.NotExtended,
                PointingState.Extended,
            },
        };

        string getSerialCode(int level)
        {
            switch (level)
            {

                case 0:
                    return "SVETLO25";
                case 1:
                    return "SVETLO50";//1
                case 2:
                    return "SVETLO75";//2
                case 3:
                    return "SVETLO";//3
                default:
                    return "TMA";//default
            }
        }


    private void setGesture(PointingState[] fingers)
    {
        Thumb = fingers[0];
        Index = fingers[1];
        Middle = fingers[2];
        Ring = fingers[3];
        Pinky = fingers[4];
    }

    private IEnumerator watcherCoroutine;

    void Awake () {
      watcherCoroutine = extendedFingerWatcher();
      if(HandModel == null){
        HandModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }

        void OnEnable() {
            sendToSerial(getSerialCode(-1));
            setGesture(gestures[0]);
      StartCoroutine(watcherCoroutine);
    }
  
    void OnDisable () {
      StopCoroutine(watcherCoroutine);
      Deactivate();
    }
  
    IEnumerator extendedFingerWatcher() {
      Hand hand;
      while(true){
        bool fingerState = false;
        if(HandModel != null && HandModel.IsTracked){
          hand = HandModel.GetLeapHand();
          if(hand != null){
                        string code;
                        if (level == -1){
                            level = 0;
                            code = getSerialCode(level);
                            sendToSerial(code);
                        }

            fingerState = matchFingerState(hand.Fingers[0], 0)
              && matchFingerState(hand.Fingers[1], 1)
              && matchFingerState(hand.Fingers[2], 2)
              && matchFingerState(hand.Fingers[3], 3)
              && matchFingerState(hand.Fingers[4], 4);
            if(HandModel.IsTracked && fingerState){

                            if (level < gestures.Length - 1)
                            {
                                level++;
                                setGesture(gestures[level]);
                            } else if(level == gestures.Length -1)
                            {
                                level++;
                            }


                            code = getSerialCode(level);
                            sendToSerial(code);

                            Activate();
            } else if(!HandModel.IsTracked || !fingerState) {
              Deactivate();
            }
          }
        } else {
                    if(level != -1 && level != gestures.Length)
                    {
                        level = -1;
                        setGesture(gestures[0]);
                        string code = getSerialCode(level);
                        sendToSerial(code);
                    }
                    
                    if (IsActive)
                    {
                        Deactivate();
                    }
        }
        yield return new WaitForSeconds(Period);
      }
    }

        private ArduinoConnector connector;




        private void sendToSerial(string code) //tady davam serial pro arduino
        {
            Debug.Log(code);
            if (connector == null) { connector = gameObject.AddComponent<ArduinoConnector>(); }

            string sound;
            switch (code)
            {
                case ("SVETLO25"):
                    sound = "SoundAura";
                    break;
                case ("SVETLO50"):
                    sound = "SoundMagic";
                    break;
                case ("SVETLO75"):
                    sound = "SoundMagic";
                    break;
                case ("SVETLO"):
                    sound = "SoundUnlock";
                    break;
                default:
                    sound = "SoundFail";
                    GameObject.Find("SoundAura").GetComponent<AudioSource>().Stop();
                    break;
            }


            GameObject.Find("SoundMagic").GetComponent<AudioSource>().Stop();
            GameObject.Find("SoundFail").GetComponent<AudioSource>().Stop();


            GameObject.Find(sound).GetComponent<AudioSource>().Play();

            connector.WriteToArduino(code);
            
        }

        private bool matchFingerState (Finger finger, int ordinal) {
      PointingState requiredState;
      switch (ordinal) {
        case 0:
          requiredState = Thumb;
          break;
        case 1:
          requiredState = Index;
          break;
        case 2:
          requiredState = Middle;
          break;
        case 3:
          requiredState = Ring;
          break;
        case 4:
          requiredState = Pinky;
          break;
        default:
          return false;
      }
            
      return (requiredState == PointingState.Either) ||
             (requiredState == PointingState.Extended && finger.IsExtended) ||
             (requiredState == PointingState.NotExtended && !finger.IsExtended);
    }

    #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos && HandModel != null) {
        Hand hand = HandModel.GetLeapHand();
        for (int f = 0; f < 5; f++) {
          Finger finger = hand.Fingers[f];
          if (matchFingerState(finger, f)) {
            Gizmos.color = Color.green;
          } else {
            Gizmos.color = Color.red;
          }
          Gizmos.DrawWireSphere(finger.TipPosition.ToVector3(), finger.Width);
        }
      }
    }
    #endif
  }
  
  /** Defines the settings for comparing extended finger states */
  public enum PointingState{Extended, NotExtended, Either}
}
