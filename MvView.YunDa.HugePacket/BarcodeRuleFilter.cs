using MvBarCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Core
{
    public class BarcodeRuleFilter
    {
        /// <summary>
        /// 检查并捕获条码数据
        /// </summary>
        /// <param name="item">条码信息</param>
        /// <param name="result">条码捕获结果</param>
        /// <returns>条码检测结果</returns>
        public bool CheckAndCatchBarcode(MvCodeInfo item, ref List<string> result,MvBarCodeGlobalVar.BarcodeRuleType barcodeType)
        {
            // 检查条形码有效性
            if (item.Valid != 0x01ff)
            {
                return false;
            }

            string tmp = new string(item.Code, 0, item.CodeLen);

            // 校验一维码字符的有效性
            if (!CheckBarCodeCharacter(tmp))
            {
                return false;
            }

            //韵达包裹条码
            if (MvBarCodeGlobalVar.BarcodeRuleType.yundaCode == barcodeType)
            {
                // 检查条形码长度
                if (!CheckCodeLen(item.CodeLen))
                {
                    return false;
                }
            }

            //韵达上一站和下一站条码
            if(MvBarCodeGlobalVar.BarcodeRuleType.yundaSiteCode==barcodeType)
            {
                // 检查条形码长度
                if ((item.CodeLen == 0x06 || item.CodeLen == 0x0b) == false)
                {
                    return false;
                }
            }


            // 捕获条码数据
            if (result.IndexOf(tmp) == -1)
            {
                result.Add(tmp);
                return true;
            }

            return false;
        }


        /// <summary>
        /// 检查条码字符
        /// </summary>
        /// <param name="code">条码信息</param>
        /// <returns>检查结果</returns>
        private bool CheckBarCodeCharacter(string code)
        {
            bool hasLine = true;

            foreach (var c in code)
            {
                if ((c - '0' >= 0 && c - '9' <= 0))
                {
                    continue;
                }
                else if (c - 'A' >= 0 && c - 'z' <= 0)
                {
                    continue;
                }
                else if (c == '-')
                {
                    hasLine = true;
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return hasLine;
        }

        /// <summary>
        /// 检查一维码长度
        /// </summary>
        /// <param name="len">被检查长度信息</param>
        /// <returns>检查结果</returns>
        private bool CheckCodeLen(int len)
        {
            return len == 0x18 || len == 0x0d || len == 0x12;
        }

        /// <summary>
        /// 条形码截取
        /// </summary>
        /// <param name="code">被截取的条码数据</param>
        /// <returns>截取结果</returns>
        private string ShortCutBarCode(char[] code)
        {
            return new string(code, 0, 0x0d);
        }

    }
}
