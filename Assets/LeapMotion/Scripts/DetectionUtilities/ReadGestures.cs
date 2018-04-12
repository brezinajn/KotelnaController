using UnityEngine;
using System.Collections;

namespace Leap.Unity
{
    public class ReadGestures : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {
            FingerDirectionDetector fd = gameObject.AddComponent<FingerDirectionDetector>();
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }

}
