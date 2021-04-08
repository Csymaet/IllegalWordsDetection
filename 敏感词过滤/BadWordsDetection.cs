
// 参考自：github: https://github.com/NewbieGameCoder/IllegalWordsDetection

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Plugins.EliUtilities.敏感词过滤
{
    public static class BadWordsDetection
    {
        private static HashSet<string> badWordSet = new HashSet<string>();
        private static byte[] charPos_InAllBadWords = new byte[char.MaxValue];  // 存了某一个字在所有敏感词中的位置，（超出8个的截断为第8个位置）
        private static byte[] lengths_StartWithKey = new byte[char.MaxValue];   // 存储了以key开关的敏感词的长度信息，超过8会截断为8
        private static byte[] maxLength_StartWithKey = new byte[char.MaxValue];
        private static BitArray isHasBadWord_EndWithKey = new BitArray(char.MaxValue);
        private static BitArray toSkipBitArray = new BitArray(char.MaxValue);
        private static readonly string toSkipList = " \t\r\n" +
                                                    "`~!@#$%^&*()_+-=[]\\;',./{}|:\"<>?"+
                                                    "·~！@#￥%……&*（）——+-=【】、；’，。、{}|：”《》？"+
                                                    "αβγδεζηθικλμνξοπρστυφχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ" +
                                                    "。，、；：？！…—·ˉ¨‘’“”々～‖∶＂＇｀｜〃〔〕〈〉《》「」『』．〖〗【】（）［］｛｝ⅠⅡⅢⅣⅤⅥⅦⅧⅨⅩⅪⅫ" +
                                                    // "⒈⒉⒊⒋⒌⒍⒎⒏⒐⒑⒒⒓⒔⒕⒖⒗⒘⒙⒚⒛㈠㈡㈢㈣㈤㈥㈦㈧㈨㈩①②③④⑤⑥⑦⑧⑨⑩⑴⑵⑶⑷⑸⑹⑺⑻⑼⑽⑾⑿⒀⒁⒂⒃⒄⒅⒆⒇" +    // 有实际意义的字符不跳过
                                                    "≈≡≠＝≤≥＜＞≮≯∷±＋－×÷／∫∮∝∞∧∨∑∏∪∩∈∵∴⊥∥∠⌒⊙≌∽√§№☆★○●◎◇◆□℃‰€■△▲※→←↑↓〓¤°＃＆＠＼︿＿￣―♂♀" +
                                                    "┌┍┎┐┑┒┓─┄┈├┝┞┟┠┡┢┣│┆┊┬┭┮┯┰┱┲┳┼┽┾┿╀╁╂╃└┕┖┗┘┙┚┛━┅┉┤┥┦┧┨┩┪┫┃┇┋┴┵┶┷┸┹┺┻╋╊╉╈╇╆╅╄";
    
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="badWords"></param>
        public static void Init(string[] badWords)
        {
            if (badWords == null || badWords.Length == 0)
                return;

            // 1. 记录SymbolsToSkip
            foreach (var @char in toSkipList)
            {
                toSkipBitArray[@char] = true;
            }
            
            foreach (var item in badWords)
            {
                string badWord = item.Trim();   // 去空格
                badWord = RemoveSymbolsToSkip(badWord); // 去符号
                if(string.IsNullOrEmpty(badWord))
                    continue;

                badWord = badWord.ToLower();  // 变小写
                
                int badWordLength = badWord.Length;

                // 2. 记录badWord每个字符的位置信息
                for (int i = 0; i < badWordLength; i++)
                {
                    if (i < 7)
                    {
                        charPos_InAllBadWords[badWord[i]] |= (byte)(1 << i);
                    }
                    else
                    {
                        charPos_InAllBadWords[badWord[i]] |= 0x80;
                    }
                }
            
                int badWordLength_MaxIs8 = Math.Min(8, badWordLength);
                int badWordLength_MaxIs255 = Math.Min(byte.MaxValue, badWordLength);
                char firstChar = badWord[0];
                char lastChar = badWord[badWordLength - 1];
            
                // 3. 记录长度，截断为8
                lengths_StartWithKey[firstChar] |= (byte) (1 << (badWordLength_MaxIs8 - 1));
                // 4. 记录最大长度，截断为255
                if (maxLength_StartWithKey[firstChar] < badWordLength_MaxIs255)
                {
                    maxLength_StartWithKey[firstChar] = (byte) (badWordLength_MaxIs255);
                }
                
                // 5. 记录最后一个字符
                isHasBadWord_EndWithKey[lastChar] = true;
            
                // 6. 记录badWords
                if (!badWordSet.Contains(badWord))
                    badWordSet.Add(badWord);
            }
        }
    
        /// <summary>
        /// 检查是否存在敏感词
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsExistBadWords(string text)
        {
            text = RemoveSymbolsToSkip(text);
            if (string.IsNullOrEmpty(text))
                return false;
            text = text.ToLower();
        
            int curCharIndex = 0;
            for (; curCharIndex < text.Length; curCharIndex++)
            {
                curCharIndex = FindFirstChar_IsFirstCharOfBadWords(text, curCharIndex);

                if (curCharIndex >= text.Length)
                {
                    return false;
                }

                if (HasBadWord_LengthIs1_StartWithKey(text[curCharIndex]))
                {
                    return true;
                }
            
                // 执行到这里，说明curChar是某个badWord的第一个字符，且这个badWord长度不为1
                if (HasBadWord_StartWithCurPos(text, curCharIndex))
                {
                    return true;
                }
            }
        
            return false;
        }
    
        /// <summary>
        /// 过滤字符串,默认遇到敏感词默认用'*'代替
        /// </summary>
        /// <param name="text"></param>
        /// <param name="mask"></param>
        public static string Filter(string text, char mask = '*')
        {
            text = RemoveSymbolsToSkip(text, out var removedDic);
            StringBuilder textSb = new StringBuilder(text);

            var badWordDic = DetectBadWords(text);

            // 敏感词替换为mask
            foreach (var badWordInfo in badWordDic)
            {
                for (int i = badWordInfo.Key; i < badWordInfo.Key + badWordInfo.Value; i++)
                {
                    textSb[i] = mask;
                }
            }

            textSb = AddRemovedSymbol(textSb, removedDic);

            return textSb.ToString();
        }

        /// <summary>
        /// 检查敏感词，返回敏感词的位置信息
        /// </summary>
        /// <param name="text_RemovedSymbolToSkip">传入已移除 toSkipList中的符号 的文本</param>
        /// <returns>返回值参数说明,result: [startIndex, length]</returns>
        private static Dictionary<int, int> DetectBadWords(string text_RemovedSymbolToSkip)
        {
            string text = text_RemovedSymbolToSkip;
            var result = new Dictionary<int, int>();

            if (string.IsNullOrEmpty(text))
                return result;
            text = text.ToLower();

            int curCharIndex = 0;
            for (; curCharIndex < text.Length; curCharIndex++)
            {
                curCharIndex = FindFirstChar_IsFirstCharOfBadWords(text, curCharIndex);

                if (curCharIndex >= text.Length)
                {
                    return result;
                }
            
                if (HasBadWord_LengthIs1_StartWithKey(text[curCharIndex]))
                {
                    result.Add(curCharIndex, 1);
                    continue;
                }
            
                if (HasBadWord_StartWithCurPos(text, curCharIndex, out int endIndex))
                {
                    result.Add(curCharIndex, endIndex - curCharIndex + 1);
                    continue;
                }
            }

            return result;
        }

        /// <summary>
        /// 从curCharIndex开始，查找一个字符，这个字符是某个badWord的首字符
        /// </summary>
        /// <param name="text"></param>
        /// <param name="curCharIndex"></param>
        /// <returns></returns>
        private static int FindFirstChar_IsFirstCharOfBadWords(string text, int curCharIndex)
        {
            while (curCharIndex < text.Length)
            {
                if (IsFirstCharOfBadWords(text[curCharIndex]))
                {
                    break;
                }
                else
                {
                    curCharIndex++;
                }
            }

            return curCharIndex;
        }

        /// <summary>
        /// 从当前位置开始，是否可找到BadWord
        /// </summary>
        /// <param name="text"></param>
        /// <param name="curCharIndex"></param>
        /// <returns></returns>
        private static bool HasBadWord_StartWithCurPos(string text, int curCharIndex)
        {
            return HasBadWord_StartWithCurPos(text, curCharIndex, out int endIndex);
        }

        /// <summary>
        /// 从当前位置开始，是否可找到BadWord
        /// </summary>
        /// <param name="text"></param>
        /// <param name="curCharIndex">前提是，处于curCharIndex的字符是某个badWord的起始字符</param>
        /// <param name="endIndex">所找到的badWord的结束索引</param>
        /// <returns></returns>
        private static bool HasBadWord_StartWithCurPos(string text, int curCharIndex, out int endIndex)
        {
            endIndex = 0;
        
            for (int i = 1; i < text.Length - curCharIndex; i++)
            {
                int maxLength = maxLength_StartWithKey[text[curCharIndex]];
                if (i >= maxLength && maxLength < byte.MaxValue)
                {
                    return false;
                }

                if (IsNthCharOfBadWords(text[curCharIndex + i], i))
                {
                    if (IsEndCharOfBadWords(text[curCharIndex + i]))
                    {
                        if (badWordSet.Contains(text.Substring(curCharIndex, i + 1)))
                        {
                            endIndex = curCharIndex + i;
                            return true;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
    
        /// <summary>
        /// 判断某字符是不是某一badWord的首字符
        /// </summary>
        /// <returns></returns>
        private static bool IsFirstCharOfBadWords(char @char)
        {
            return maxLength_StartWithKey[@char] > 0;
        }

        /// <summary>
        /// 是否有一个敏感词的长度为1，且以传入的字符开头
        /// </summary>
        /// <returns></returns>
        private static bool HasBadWord_LengthIs1_StartWithKey(char @char)
        {
            return (lengths_StartWithKey[@char] & 0x01) == 0x01;
        }

        /// <summary>
        /// 判断某字符是不是某一badWord的第n个字符
        /// </summary>
        /// <param name="char"></param>
        /// <param name="nth">n从0开始计数</param>
        /// <returns></returns>
        private static bool IsNthCharOfBadWords(char @char, int nth)
        {
            byte value1 = charPos_InAllBadWords[@char];
            byte value2 = (byte) (1 << nth);
            return (value1 & value2)  == value2;
        }

        /// <summary>
        /// 判断某字符是否是某badWord的尾字符
        /// </summary>
        /// <param name="char"></param>
        /// <returns></returns>
        private static bool IsEndCharOfBadWords(char @char)
        {
            return isHasBadWord_EndWithKey[@char];
        }

        /// <summary>
        /// 移除需要移除的字符
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string RemoveSymbolsToSkip(string text)
        {
            return RemoveSymbolsToSkip(text, out var removedDic);
        }

        /// <summary>
        /// 移除需要移除的字符
        /// </summary>
        /// <param name="text"></param>
        /// <param name="removedDic">包含了被移除字符的信息，参数：[索引, 字符]</param>
        /// <returns></returns>
        private static string RemoveSymbolsToSkip(string text, out List<KeyValuePair<int, char>> removedDic)
        {
            removedDic = new List<KeyValuePair<int, char>>();
        
            StringBuilder textSb = new StringBuilder();

            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (!toSkipBitArray[text[i]])
                {
                    textSb.Insert(0, text[i]);
                }
                else
                {
                    removedDic.Add(new KeyValuePair<int, char>(i, text[i]));
                }
            }

            return textSb.ToString();
        }

        /// <summary>
        /// 将被移除的符号添加回来
        /// </summary>
        /// <param name="textSb"></param>
        /// <param name="removedDic"></param>
        /// <returns></returns>
        private static StringBuilder AddRemovedSymbol(StringBuilder textSb, List<KeyValuePair<int, char>> removedDic)
        {
            for (int i = removedDic.Count - 1; i >= 0; i--)
            {
                textSb.Insert(removedDic[i].Key, removedDic[i].Value);
            }

            return textSb;
        }
    }
}
