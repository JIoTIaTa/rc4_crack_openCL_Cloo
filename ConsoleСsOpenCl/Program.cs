using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloo;
using System.Runtime.InteropServices;

namespace ConsoleСsOpenCl
{

    /**
* Crypting data class (CPU)
*/
    class RC_4
    {
        public RC_4(int s_length, uint key_length, int data_length, int Order)
        {
            this.S_Length = s_length;
            this.Key_length = key_length;
            this.inData_length = data_length;
            this.order = Order;            
        }
        public RC_4(double s_length, long key_length, char data_length, float Order)
        {
            this.S_Length = (int)s_length;
            this.Key_length = (uint)key_length;
            this.inData_length = data_length;
            this.order = (int)Order;
        }
        public RC_4(uint key_length)
        {            
            this.Key_length = (uint)key_length;            
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
         public void crypting(ref byte[] inData, ref byte[] outData)
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
        const Int16 MEM_SIZE = 128;
        const Int64 MAX_SOURCE_SIZE  = 0x100000;
        const int CL_SUCCESS = 0;
        static void Main(string[] args)
        {
            
            int inData_length = 7;
            int order = 24;
            Console.WriteLine("Please, write exponentiation order\n");
            order = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Order exponentiation:{0}\n", order);
            byte [] inText = new byte[inData_length];
            byte[] cryptText = new byte[inData_length];
            byte[] key = new byte[10];
            byte[] outData = new byte[7];
            inText[0] = 5;
            inText[1] = 5;
            inText[2] = 5;
            inText[3] = 5;
            inText[4] = 5;
            inText[5] = 5;
            inText[6] = 5;
            Console.WriteLine("Data Encryption started...\n");
            //RC_4 rc4(256, 9, inData_length, order);
            RC_4 rc4 = new RC_4(256, 9, inData_length, order);
             
            rc4.crypting(ref inText, ref cryptText);
            Console.WriteLine("Data Encryption finished\n");
            //кінець ініціалізації

            //ініціалізація даних openCL
            //cl_context context = NULL; 
            ComputeContextPropertyList contextPropetyList;
            List<ComputeDevice> Devs = new List<ComputeDevice>();
            ComputeContext context; // контекст (для управління об'єктами, такими як командних черг, пам'яті, програми та об'єктів ядра, і для виконання ядра на один або кілька пристроїв)
            ComputeCommandQueue command_queue; //команда 
            ComputeProgram  program; // програма (компіляція і лінковка всіх кодів в файлах .сl)
            ComputeKernel kernel; //  робить з функцій з ідендифікаторами __kernel реальний kernel :)
            string[] platform_id ; // id доступних платформ	
            uint ret_num_platforms = 0; //кількість доступних платформ
            uint ret = 0; // флажок помилки
            string[] cdDevices; // список id доступних пристроїв
            uint ciDeviceCount = 0; // кількість доступних пристроїв
            const Int32 szGlobalWorkSize = 81920; // загальна кількість work-items, які будуть виконуватись
            Int64 [] offset = new Int64[1];
            const Int16 GroupWorkSize = 128; // ниток в блоці

            ComputeBuffer<byte> _devinText;
            ComputeBuffer<byte> _devkey;
            ComputeBuffer<byte> _devoutData;
            ComputeBuffer<byte> _devcorrectDataTemp;
            ComputeBuffer<Int64> _devoffset;

            //кінець ініціалізації

            //запис коду ядра в строку, яка буде виконуватись
            //char             string[MEM_SIZE];
            string source_str = @"
            typedef unsigned char  u8;
            typedef unsigned short u16;
            typedef unsigned int   u32;
            typedef unsigned long  u64;

            typedef struct
            {
	            u8 S[256];	
            } RC4_S;

            void swap(__local RC4_S *rc4_s, const u8 i, const u8 j)
            {
	            u8 tmp;
	            tmp = rc4_s->S[i];
	            rc4_s->S[i] = rc4_s->S[j];
	            rc4_s->S[j] = tmp;
            }

            void rc4_init(__local RC4_S *rc4_s, const u8 key[8])
            {
	            //u32 v = 0x03020100;
	            //u32 a = 0x04040404;

	            //__local u32 *ptr = (__local u32 *) rc4_s->S;	

	            #ifdef _unroll
	            #pragma unroll
	            #endif
	            //for (u32 i = 0; i < 64; i++)
	            //{
	            //	//ptr[i] = v; v = v + a;
	            //	//*ptr++ = v; v = v + a;
	            //}
	            for (u16 i = 0; i < 256; i++)
	            {
		            rc4_s->S[i] = i;
	            }

	            u32 j = 0;
	            u32 g = 0;
	            u32 h = 0;
	            u8 temp = 0x00;

	            #ifdef _unroll
	            #pragma unroll
	            #endif
	            for (g = h = 0; g < 252; g += 9)
	            {		
		            h = (h + rc4_s->S[g + 0] + key[0]);	swap(rc4_s, g + 0, h);		
		            h = (h + rc4_s->S[g + 1] + key[1]);	swap(rc4_s, g + 1, h);		
		            h = (h + rc4_s->S[g + 2] + key[2]);	swap(rc4_s, g + 2, h);
		            h = (h + rc4_s->S[g + 3] + key[3]);	swap(rc4_s, g + 3, h);
		            h = (h + rc4_s->S[g + 4] + key[4]);	swap(rc4_s, g + 4, h);
		            h = (h + rc4_s->S[g + 5] + key[5]);	swap(rc4_s, g + 5, h);
		            h = (h + rc4_s->S[g + 6] + key[6]);	swap(rc4_s, g + 6, h);
		            h = (h + rc4_s->S[g + 7] + key[7]);	swap(rc4_s, g + 7, h);
		            h = (h + rc4_s->S[g + 8] + key[8]);	swap(rc4_s, g + 8, h);
	            }
	            h = (h + rc4_s->S[252] + key[0]); swap(rc4_s, 252, h);
	            h = (h + rc4_s->S[253] + key[1]); swap(rc4_s, 253, h);
	            h = (h + rc4_s->S[254] + key[2]); swap(rc4_s, 254, h);
	            h = (h + rc4_s->S[255] + key[3]); swap(rc4_s, 255, h);

	            for (g = h = 0; g < 252; g += 9)
	            {
		            h = (h + rc4_s->S[g + 0] + key[0]);	swap(rc4_s, g + 0, h);
		            h = (h + rc4_s->S[g + 1] + key[1]);	swap(rc4_s, g + 1, h);
		            h = (h + rc4_s->S[g + 2] + key[2]);	swap(rc4_s, g + 2, h);
		            h = (h + rc4_s->S[g + 3] + key[3]);	swap(rc4_s, g + 3, h);
		            h = (h + rc4_s->S[g + 4] + key[4]);	swap(rc4_s, g + 4, h);
		            h = (h + rc4_s->S[g + 5] + key[5]);	swap(rc4_s, g + 5, h);
		            h = (h + rc4_s->S[g + 6] + key[6]);	swap(rc4_s, g + 6, h);
		            h = (h + rc4_s->S[g + 7] + key[7]);	swap(rc4_s, g + 7, h);
		            h = (h + rc4_s->S[g + 8] + key[8]);	swap(rc4_s, g + 8, h);
	            }
	            h = (h + rc4_s->S[252] + key[0]); swap(rc4_s, 252, h);
	            h = (h + rc4_s->S[253] + key[1]); swap(rc4_s, 253, h);
	            h = (h + rc4_s->S[254] + key[2]); swap(rc4_s, 254, h);
	            h = (h + rc4_s->S[255] + key[3]); swap(rc4_s, 255, h);
            }


            __kernel void bit_key_finder(__global  unsigned char *inData, __global unsigned char * key, __global unsigned char * outData, __global unsigned char * correctDataTemp, __global ulong* offset)
            {
	            u64 globID = get_global_id(0); //індекс нитки в глобаьній сітці
	            u32  locID = get_local_id(0); // індекс нитки в блоці
	            size_t i = (81920 * offset[0]) + globID; // глобальний зсув кожної іттерації

	            if (i > 1099511627776) // 2^40
	            {
		            return;
	            }

	            u8 privatKey[9];
	            u8 privatoutData[7];
	            u32 general_g = 0;
	            u32 general_h = 0;
	            __local RC4_S rc4_gamma[128]; // __local - ідентифікатор для shared пам'яті. Використовується 32768 байт shared пам'яті на блок
	            __local RC4_S *rc4_s = &rc4_gamma[locID];		

	            privatKey[0] = i >> 32 & 0xFF;
	            privatKey[1] = i >> 24 & 0xFF;
	            privatKey[2] = i >> 16 & 0xFF;
	            privatKey[3] = i >> 8 & 0xFF;
	            privatKey[4] = i & 0xFF;
	            privatKey[5] = 0x00;
	            privatKey[6] = 0x00;
	            privatKey[7] = 0x00;
	            privatKey[8] = 0x00;

	            rc4_init(rc4_s, privatKey);	// ініціалізаці і перемішування

	            #ifdef _unroll
	            #pragma unroll
	            #endif
	            for (u32 q = 0; q < 7; q++)
	            {
		            u8 tmp = 0x00;
		            general_g = (general_g + 1) % 256;
		            general_h = (general_h + rc4_s->S[general_g]) % 256; swap(rc4_s, general_g, general_h);		
		            tmp = rc4_s->S[((rc4_s->S[general_g] + rc4_s->S[general_h]) % 256)];
		            privatoutData[q] = inData[q] ^ tmp;
	            }


	            if (((privatoutData[0] == correctDataTemp[0]) &&
		            (privatoutData[1] == correctDataTemp[1]) &&
		            (privatoutData[2] == correctDataTemp[2]) &&
		            (privatoutData[3] == correctDataTemp[3]) &&
		            (privatoutData[4] == correctDataTemp[4]) &&
		            (privatoutData[5] == correctDataTemp[5]) &&
		            (privatoutData[6] == correctDataTemp[6])))
	            {
		            for (int j = 0; j < 9; j++)
		            {
			            key[j] = privatKey[j];
		            }
		            key[9] = 1;		// зайвий байт - флажок done
		            for (int j = 0; j < 7; j++)
		            {
			            outData[j] = privatoutData[j];
		            }
		            return;
	            }
            }";
            // строка з кодом готова


            /*доступнi платформи */
            ret_num_platforms = (uint)ComputePlatform.Platforms.Count;
            platform_id = new string[ret_num_platforms];
            for (int i = 0; i < ret_num_platforms; i++)
            {
                platform_id[i] = ComputePlatform.Platforms[i].Name;
                Console.WriteLine("> OpenCL Platform #{0}  cl_platform_id: {1}\n", i, platform_id[i]); // вывести имена всех доступых платформ
            }
                
            //if (ret != CL_SUCCESS)
            //{
            //    Console.WriteLine("Error: Failed to check OpenCL platforms!\n");
            //}

            /*доступні GPU OpenCL devices*/
            contextPropetyList = new ComputeContextPropertyList(ComputePlatform.Platforms[1]);
            context = new ComputeContext(ComputeDeviceTypes.Gpu, contextPropetyList, null, IntPtr.Zero);
            ciDeviceCount = (uint)context.Devices.Count; // получить количесто доступных GPU devices
            cdDevices = new string[ciDeviceCount]; // создать масив для записи id  доступых GPU devices
            for (int i = 0; i < ciDeviceCount; i++)
            {
                cdDevices[i] = context.Devices[i].Name;
                Devs.Add(ComputePlatform.Platforms[1].Devices[i]);
                Console.WriteLine("> OpenCL Devices #{0}  device_name: {1}\n", i, cdDevices[i]); // вывести имена всех доступых GPU devices               
            }


            //засікаємо час роботи            
            var Time = System.Diagnostics.Stopwatch.StartNew();

            /* створюємо контекст */
            //Контекст OpenCL створюється за допомогою одного або декількох пристроїв. Контексти використовуються під час виконання OpenCL для управління об'єктами, такими як командних черг, пам'яті, програми та об'єктів ядра, і для виконання ядра на один або кілька пристроїв, зазначених в даному контексті.

            /* створюємо команду */
            command_queue = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);

            /* створення буферів з даними */
            _devinText = new ComputeBuffer<byte>(context, ComputeMemoryFlags.ReadWrite, cryptText);
            _devkey = new ComputeBuffer<byte>(context, ComputeMemoryFlags.ReadWrite, key); // 10 байт - флажок done
            _devoutData = new ComputeBuffer<byte>(context, ComputeMemoryFlags.ReadWrite, outData);
            _devcorrectDataTemp = new ComputeBuffer<byte>(context, ComputeMemoryFlags.ReadOnly, inText);
            
            


            /* Створення програми з хоста*/
            program = new ComputeProgram(context, new string[] { source_str });

            /* Побудова програми (компіляція і лінковка Kernel)*/
            program.Build(Devs, "", null, IntPtr.Zero);

            /* Створення OpenCL Kernel */
            //Ініціалізація нової програми
            kernel = program.CreateKernel("bit_key_finder");


            /* Відправка параметрів OpenCL Kernel*/
            kernel.SetMemoryArgument(0, _devinText);
            kernel.SetMemoryArgument(1, _devkey);
            kernel.SetMemoryArgument(2, _devoutData);
            kernel.SetMemoryArgument(3, _devcorrectDataTemp);           
                        

            do
            {
                // ret = clSetKernelArg(kernel, 4, sizeof(size_t), (void*)&offset);      
                _devoffset = new ComputeBuffer<Int64>(context, ComputeMemoryFlags.ReadWrite, offset);
                kernel.SetMemoryArgument(3, _devoffset);
                command_queue.Execute(kernel, null,  new long[] { szGlobalWorkSize }, new long[] { GroupWorkSize }, null);
                GCHandle keyHandle = GCHandle.Alloc(key, GCHandleType.Pinned);
                command_queue.Read<byte>(_devkey, true, 0, 10 , keyHandle.AddrOfPinnedObject(), null);
                offset[0]++;
            } while (key[9] != 1); // 10-й байт ключа - флажок!

            Time.Stop();
            Console.WriteLine("Time: {0} milliseconds\n",Time.Elapsed);
            
            /*Зчитаємо результат з пристрою (тут синхронно(CL_TRUE))*/
                        
            Console.WriteLine("Key: |");
            for (int i = 0; i < 9; i++)
            {
                Console.Write(key[i]);
                Console.Write("|");       
            }
            Console.Write("\n");
        }
    }
}
