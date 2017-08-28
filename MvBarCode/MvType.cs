using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvBarCode
{
    /// <summary>
    /// 图像处理类型
    /// </summary>
    public enum MvsImgType
    {
        /// <summary>
        /// 逐行扫描图像, Y平面保存格式
        /// </summary>
        MVS_IMGTP_UITL_Y = 0x0100,
		
        /// <summary>
        /// RGB平面保存格式
        /// </summary>
        MVS_IMGTP_UITL_RGB = 0x0200,
		
        /// <summary>
        /// RGB 24平面保存格式 - R在一个平面，G在一平面，B在一个平面
        /// </summary>
        MVS_IMGTP_UITL_RGBP_24 = 0x0201,
		
        /// <summary>
        /// HSI 平面保存格式
        /// </summary>
        MVS_IMGTP_UITL_HSI = 0x0400,
		
        /// <summary>
        /// LAB 平面保存格式
        /// </summary>
        MVS_IMGTP_UITL_LAB = 0x0500,
		
        /// <summary>
        /// LAB 平面保存格式
        /// </summary>
        MVS_IMGTP_UITL_DIF = 0x0600,
		
        /// <summary>
        /// XYZ 平面保存格式
        /// </summary>
        MVS_IMGTP_UITL_XYZ = 0x0700,
		
        /// <summary>
        /// UITL_YUV
        /// </summary>
        MVS_IMGTP_UITL_YUV = 0x1000,
		
        /// <summary>
        /// YUV 420平面保存格式 - YUV是一个平面
        /// </summary>
        MVS_IMGTP_UITL_YUV_420,
		
        /// <summary>
        /// YUV 420平面保存格式 - Y是一个平面，U是一个平面，V也是一个平面
        /// </summary>
        MVS_IMGTP_UITL_YUVP_420,
		
        /// <summary>
        /// YUV 420半平面保存格式 - Y在一个平面，UV在另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_UITL_YUVSP_420,
		
        /// <summary>
        /// YUV 420半平面保存格式 - Y在一个平面，VU在另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_UITL_YVUSP_420,
		
        /// <summary>
        /// YUV 422平面保存格式 - YUV是一个平面
        /// </summary>
        MVS_IMGTP_UITL_YUV_422,
		
        /// <summary>
        /// YUV 422平面保存格式 - Y是一个平面，V是一个平面，U也是一个平面
        /// </summary>
        MVS_IMGTP_UITL_YUVP_422,
		
        /// <summary>
        /// YUV 422半平面保存格式 - Y是一个平面，UV是另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_UITL_YUVSP_422,
		
        /// <summary>
        /// YUV 422半平面保存格式 - Y在一个平面，VU在另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_UITL_YVUSP_422,

        /// <summary>
        /// 隔行扫描图像，ITL_YUV
        /// </summary>
        MVS_IMGTP_ITL_YUV = 0x3000,
		
        /// <summary>
        /// YUV 420平面保存格式 - Y是一个平面，U是一个平面，V也是一个平面
        /// </summary>
        MVS_IMGTP_ITL_YUVP_420,
		
        /// <summary>
        /// YUV 420半平面保存格式 - Y在一个平面，UV在另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_ITL_YUVSP_420,
		
        /// <summary>
        /// YUV 420半平面保存格式 - Y在一个平面，VU在另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_ITL_YVUSP_420,
		
        /// <summary>
        /// YUV 422平面保存格式 - Y是一个平面，V是一个平面，U也是一个平面
        /// </summary>
        MVS_IMGTP_ITL_YUVP_422,
		
        /// <summary>
        /// YUV 422半平面保存格式 - Y是一个平面，UV是另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_ITL_YUVSP_422,
		
        /// <summary>
        /// YUV 422半平面保存格式 - Y在一个平面，VU在另一平面（交错保存）
        /// </summary>
        MVS_IMGTP_ITL_YVUSP_422
    }

    public enum MvsImgDataType
    {
        /// <summary>
        /// 无符号8位
        /// </summary>
        MVS_IMGDTP_U8 = 0,
		
        /// <summary>
        /// 有符号8位
        /// </summary>
        MVS_IMGDTP_S8,
		
        /// <summary>
        /// 有符号32位
        /// </summary>
        MVS_IMGDTP_S32,
		
        /// <summary>
        /// 无符号32位
        /// </summary>
        MVS_IMGDTP_U32,
		
        /// <summary>
        /// 有符号16位
        /// </summary>
        MVS_IMGDTP_S16,
		
        /// <summary>
        /// 无符号16位
        /// </summary>
        MVS_IMGDTP_U16,
		
        /// <summary>
        /// 浮点32位
        /// </summary>
        MVS_IMGDTP_F32,
		
        /// <summary>
        /// 浮点64位
        /// </summary>
        MVS_IMGDTP_F64
    }

    public enum MvsObjectType
    {
        /// <summary>
        /// 点
        /// </summary>
        MVS_OBJTP_POINT,
		
        /// <summary>
        /// 线段
        /// </summary>
        MVS_OBJTP_LINE_SEG,
		
        /// <summary>
        /// 线
        /// </summary>
        MVS_OBJTP_LINE,
		
        /// <summary>
        /// 圆
        /// </summary>
        MVS_OBJTP_CIRCLE,
		
        /// <summary>
        /// 椭圆
        /// </summary>
        MVS_OBJTP_ELLIPSE,
		
        /// <summary>
        /// 图像
        /// </summary>
        MVS_OBJTP_IMG
    }
}
