using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using ThridLibray;

namespace MvView.Core
{
    public class LocalFrameData : IGrabbedRawData
    {
        private int height = 0;
        private int width = 0;
        Bitmap bitmap;
        byte[] bitmapData;
        private IntPtr imagePointer = IntPtr.Zero;
        private int depth = 0;
        private PixelFormat pixel;
        //~LocalFrameData()
        //{
        //    if (imagePointer != null || imagePointer != IntPtr.Zero)
        //    {
        //        Marshal.FreeHGlobal(imagePointer);
        //    }
        //}
        public LocalFrameData(int width, int height, Bitmap bitmap, IntPtr raw)
        {
            this.Width = width;
            this.Height = height;
            this.LocalBitmap = bitmap;
            this.Raw = raw;
            this.depth = Bitmap.GetPixelFormatSize(bitmap.PixelFormat);
            this.pixel = bitmap.PixelFormat;
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }
        public int Width
        {
            get
            {
                return width;
            }
            set { width = value; }
        }

        public IntPtr Raw
        {
            set
            {
                imagePointer = value;
            }
            get
            {
                return imagePointer;
            }
        }

        public Bitmap LocalBitmap
        {
            set { bitmap = value; }
            get { return bitmap; }
        }
        public Bitmap ToBitmap(bool color)
        {
            return LocalBitmap;
        }

        public IGrabbedRawData Clone()
        {
            Bitmap destination = new Bitmap(Width, Height, pixel);

            BitmapData destination_bitmapdata = null;
            destination_bitmapdata = destination.LockBits(new Rectangle(0, 0, destination.Width, destination.Height), ImageLockMode.ReadWrite, destination.PixelFormat);

            unsafe
            {
                byte* source_ptr = (byte*)this.Raw;
                byte* destination_ptr = (byte*)destination_bitmapdata.Scan0;

                for (int i = 0; i < (Width * Height * (depth / 8)); i++)
                {
                    *destination_ptr = *source_ptr;
                    source_ptr++;
                    destination_ptr++;
                }
            }
            destination.UnlockBits(destination_bitmapdata);

            return new LocalFrameData(this.Width, this.Height, destination, destination_bitmapdata.Scan0);

        }


        public long BlockID
        {
            get
            {
                throw new NotImplementedException();
            }
        }



        public byte[] Image
        {
            get
            {
                return bitmapData;
            }
            set
            {
                bitmapData = value;
            }
        }

        public int ImageSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public GvspPixelFormatType PixelFmt
        {
            get
            {
                throw new NotImplementedException();
            }
        }



        public void Show(IntPtr pWnd)
        {
            //throw new NotImplementedException();
        }

        public void Show(IntPtr pWnd, float angle)
        {
            //throw new NotImplementedException();
        }

    }

}
