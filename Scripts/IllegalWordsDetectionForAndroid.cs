using UnityEngine;

namespace EliTools.IllegalWordsDetection.Scripts
{
    public class IllegalWordsDetectionForAndroid : AndroidJavaProxy 
    {
        public IllegalWordsDetectionForAndroid(string javaInterface) : base(javaInterface)
        {
        }

        public bool IsExistIllegalWords(string text)
        {
            return IllegalWordsDetection.IsExistIllegalWords(text);
        }
    }
}