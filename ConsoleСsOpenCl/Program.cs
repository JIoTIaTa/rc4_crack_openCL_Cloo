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
        RC4(int s_length, uint key_length, int data_length, int Order)
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
        private byte S = new byte[S_Length];
	    int clas_i = 0, clas_j = 0;
        void swap(uint first_bit, usecond_bit)
        {
            byte temp = S[first_bit];
            S[first_bit] = S[second_bit];
            S[second_bit] = temp;
        }

        void S_init()
        {
            for (int i = 0; i < S_Length; ++i) { S[i] = i; }
        }        
	//RC_4(int s_length, int key_length, int data_length, int Order) :S_Length(s_length), Key_length(key_length), inData_length(data_length), order(Order) { } // користувацький конструктор класу	

        unsigned char* key = new unsigned char[Key_length];

	void key_for_crypt(int m)
        {
            key[5] = 0;
            key[6] = 0;
            key[7] = 0;
            key[8] = 0;
            uint64_t pow_coef = pow(2, m);
            for (uint64_t i = 0; i <= pow_coef; i++)
            {
                key[0] = i >> 32 & 0xFF;
                key[1] = i >> 24 & 0xFF;
                key[2] = i >> 16 & 0xFF;
                key[3] = i >> 8 & 0xFF;
                key[4] = i & 0xFF;
            }
        }

        void crypting(unsigned char* inData, unsigned char* outData)
        {
            char res;
            S_init();
            key_for_crypt(order);
            rc4_init();
            rc4_init();
            for (int m = 0; m < inData_length; m++)
            {
                res = inData[m] ^ rc4_output();
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
            int i, j;
            for (i = j = 0; i < S_Length; ++i)
            {
                j = (j + key[i % Key_length] + S[i]) % S_Length;
                swap(i, j);
            }
        }


        /**
        Crypt 1 symbol
        */
        unsigned char rc4_output()
        {
            int tmp;
            clas_i = (clas_i + 1) % S_Length;
            clas_j = (clas_j + S[clas_i]) % S_Length;
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
