using UnityEngine;

namespace EliTools.IllegalWordsDetection.Scripts
{
    public class BadWordsDetectionForAndroid : AndroidJavaProxy 
    {
        public BadWordsDetectionForAndroid(string javaInterface) : base(javaInterface)
        {
        }

        public bool IsExistBadWords(string text)
        {
            return BadWordsDetection.IsExistBadWords(text);
        }
    }
}