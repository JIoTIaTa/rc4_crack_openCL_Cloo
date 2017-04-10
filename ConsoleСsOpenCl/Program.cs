using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloo;

namespace ConsoleСsOpenCl
{
    /**
* Crypting data class (CPU)
*/
    class RC_4
    {
        RC_4(int s_length, uint key_length, int data_length, int Order)
        {
            this.S_Length = s_length;
            this.Key_length = key_length;
            this.inData_length = data_length;
            this.order = Order;            
        }
        private int S_Length;
        private int inData_length;
        private int order;
        private uint Key_length;
        private byte [] S = new byte[256];
	    uint clas_i = 0, clas_j = 0;
        void swap(uint first_bit, uint second_bit)
        {
            byte temp = S[first_bit];
            S[first_bit] = S[second_bit];
            S[second_bit] = temp;
        }

        void S_init()
        {
            for (int i = 0; i < S_Length; ++i) { S[i] = (byte)i; }
        }    
        byte [] key = new byte[9];

	void key_for_crypt(int m)
        {
            key[5] = 0;
            key[6] = 0;
            key[7] = 0;
            key[8] = 0;
            UInt64 pow_coef = (UInt64)Math.Pow(2, m);
            for (UInt64 i = 0; i <= pow_coef; i++)
            {
                key[0] = (byte)(i >> 32 & 0xFF);
                key[1] = (byte)(i >> 24 & 0xFF);
                key[2] = (byte)(i >> 16 & 0xFF);
                key[3] = (byte)(i >> 8 & 0xFF);
                key[4] = (byte)(i & 0xFF);
            }
        }        
        unsafe void crypting(byte* inData, byte* outData)
        {
            byte res;
            S_init();
            key_for_crypt(order);
            rc4_init();
            rc4_init();
            for (int m = 0; m < inData_length; m++)
            {
                res = (byte)(inData[m] ^ rc4_output());
                outData[m] = res;
            }
            clas_i = 0;
            clas_j = 0;
        }

        /**
        mixing Gamma
        */
        void rc4_init()
        {
            uint i, j;
            for (i = j = 0; i < S_Length; ++i)
            {
                j = (uint)((j + key[i % Key_length] + S[i]) % S_Length);
                swap(i, j);
            }
        }


        /**
        Crypt 1 symbol
        */
        byte rc4_output()
        {
            byte tmp;
            clas_i = (uint)((clas_i + 1) % S_Length);
            clas_j = (uint)((clas_j + S[clas_i]) % S_Length);
            swap(clas_i, clas_j);
            tmp = S[(S[clas_i] + S[clas_j]) % S_Length];
            return tmp;
        }
    };
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
