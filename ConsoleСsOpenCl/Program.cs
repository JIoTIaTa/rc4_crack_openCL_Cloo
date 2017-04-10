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
            
            int inData_length = 7;
            int order = 24;
            Console.WriteLine("Please, write exponentiation order\n");
            cin >> order;
            printf("Order exponentiation:2^%.0i\n", order);
            unsigned char* inText = new unsigned char[inData_length];
            unsigned char* cryptText = new unsigned char[inData_length];
            unsigned char key[10];
            unsigned char outData[7];
            inText[0] = 5;
            inText[1] = 5;
            inText[2] = 5;
            inText[3] = 5;
            inText[4] = 5;
            inText[5] = 5;
            inText[6] = 5;
            printf("Data Encryption started...\n");
            RC_4 rc4(256, 9, inData_length, order);
            rc4.crypting(inText, cryptText);
            printf("Data Encryption finished\n");
            //кінець ініціалізації

            //ініціалізація даних openCL
            cl_context context = NULL; // контекст (для управління об'єктами, такими як командних черг, пам'яті, програми та об'єктів ядра, і для виконання ядра на один або кілька пристроїв)
            cl_command_queue command_queue = NULL; //команда 
            cl_program program = NULL; // програма (компіляція і лінковка всіх кодів в файлах .сl)
            cl_kernel kernel = NULL; //  робить з функцій з ідендифікаторами __kernel реальний kernel :)
            cl_platform_id* platform_id = NULL; // id доступних платформ	
            cl_uint ret_num_platforms = 0; //кількість доступних платформ
            cl_int ret; // флажок помилки
            cl_device_id* cdDevices = NULL; // список id доступних пристроїв
            cl_uint ciDeviceCount = 0; // кількість доступних пристроїв
            const size_t szGlobalWorkSize = 81920; // загальна кількість work-items, які будуть виконуватись
            size_t offset = 0;
            const size_t GroupWorkSize = 128; // ниток в блоці
                                              //кінець ініціалізації

            //запис коду ядра в строку, яка буде виконуватись
            char             string[MEM_SIZE];
            FILE* fp; // файл .сl з __kernel кодом 
                      //char fileName[] = "./rc4_cracker.cl";
            char fileName[] = "./rc4_cracker_vol2.cl";
            char* source_str; // строка з __kernel кодом 
            size_t source_size; // розмір строки

            /* Загружаємо kernel-код з файлу і записуємо його в строку, визначаємо довжину строки	 */
            fp = fopen(fileName, "r");
            if (!fp)
            {
                fprintf(stderr, "Failed to load kernel.\n");
                exit(1);
            }
            source_str = (char*)malloc(MAX_SOURCE_SIZE);
            source_size = fread(source_str, 1, MAX_SOURCE_SIZE, fp);
            fclose(fp);
            // строка з кодом готова


            /*доступнi платформи */
            ret = clGetPlatformIDs(NULL, NULL, &ret_num_platforms);
            platform_id = (cl_platform_id*)malloc(ret_num_platforms * sizeof(cl_platform_id));
            ret = clGetPlatformIDs(ret_num_platforms, platform_id, NULL);
            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to check OpenCL platforms!\n");
                system("pause");
                return ret;
            }
            char(*cPlatformName)[256] = new char[ret_num_platforms][256];
            for (int i = 0; i < (int)ret_num_platforms; i++)
            {
                clGetPlatformInfo(platform_id[i], CL_PLATFORM_NAME, sizeof(cPlatformName[i]), &cPlatformName[i], NULL);
                printf("> OpenCL Platform #%d (%s) cl_platform_id: %d\n", i, cPlatformName[i], platform_id[i]); // вывести имена всех доступых платформ
            }

            /*доступні GPU OpenCL devices*/
            ret = clGetDeviceIDs(platform_id[2], CL_DEVICE_TYPE_GPU, 0, NULL, &ciDeviceCount); // получить количесто доступных GPU devices
            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to check OpenCL devices!\n");
                system("pause");
                return ret;
            }
            cdDevices = (cl_device_id*)malloc(ciDeviceCount * sizeof(cl_device_id)); // создать масив для записи id  доступых GPU devices
            ret = clGetDeviceIDs(platform_id[2], CL_DEVICE_TYPE_GPU, ciDeviceCount, cdDevices, NULL); // получить все id всех доступых GPU devices
            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to get OpenCL devices id!\n");
                system("pause");
                return ret;
            }
            char(*cDevicesName)[256] = new char[ciDeviceCount][256];
            printf("Detected %d OpenCL devices of type CL_DEVICE_TYPE_GPU\n", ciDeviceCount);
            for (int i = 0; i < (int)ciDeviceCount; i++)
            {
                clGetDeviceInfo(cdDevices[i], CL_DEVICE_NAME, sizeof(cDevicesName[i]), &cDevicesName[i], NULL);
                printf("> OpenCL Device #%d (%s), cl_device_id: %d\n", i, cDevicesName[i], cdDevices[i]); // вывести имена и id всех доступых GPU devices
            } (&platform_id);

            //засікаємо час роботи
            float start_time = clock();// початковий час

            /* створюємо контекст */
            //Контекст OpenCL створюється за допомогою одного або декількох пристроїв. Контексти використовуються під час виконання OpenCL для управління об'єктами, такими як командних черг, пам'яті, програми та об'єктів ядра, і для виконання ядра на один або кілька пристроїв, зазначених в даному контексті.
            context = clCreateContext(NULL, 1, &cdDevices[0], NULL, NULL, &ret); // (список платформ та їх id що будуть використані при запуску ядра, кількість пристроїв, показчик на список id пристроїв, функія зворотнього виклику(для помилок, асинхронної реалізації openCL...)(user_data флажок поставки призначених для користувача даних), user_data (може бути NULL), повертає error)

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to create OpenCL context!\n");
                system("pause");
                return ret;
            }

            /* створюємо команду */
            command_queue = clCreateCommandQueue(context, cdDevices[0], 0, &ret); // (створений контекст, id пристрою(має бути вказаний в списку контексту), властивості(в якому порядку виконуються команди, профілювання команд), повертає error)

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to create OpenCL command!\n");
                system("pause");
                return ret;
            }

            /* створення буферів з даними */
            cl_mem _devinText = clCreateBuffer(context, CL_MEM_READ_WRITE, sizeof(unsigned char) * 7, NULL, &ret); // (створений контекст, флаги специфікації пам'яті, size,	показчик на буфер хоста (звідти заберуться дані?), повертає error)
            cl_mem _devkey = clCreateBuffer(context, CL_MEM_READ_WRITE, sizeof(unsigned char) * 10, NULL, &ret); // 10 байт - флажок done
            cl_mem _devoutData = clCreateBuffer(context, CL_MEM_READ_WRITE, sizeof(unsigned char) * 7, NULL, &ret);
            cl_mem _devcorrectDataTemp = clCreateBuffer(context, CL_MEM_READ_ONLY, sizeof(unsigned char) * 7, NULL, &ret);

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to create OpenCL buffers!\n");
                system("pause");
                return ret;
            }

            /* Створення програми з хоста*/
            program = clCreateProgramWithSource(context, 1, (const char**)&source_str,	(const size_t*)&source_size, &ret); // (створений контекст,розмір масиву строк,масив строк файлів з kernel кодом, які будуть запускатися,масив з довжинами строк, повертає error )

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to create OpenCL program!\n");
                system("pause");
                return ret;
            }

            /* Побудова програми (компіляція і лінковка Kernel)*/
            ret = clBuildProgram(program, 1, &cdDevices[0], NULL, NULL, NULL);

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to build OpenCL program!\n");
                system("pause");
                return ret;
            }

            /* Створення OpenCL Kernel */
            kernel = clCreateKernel(program, "bit_key_finder", &ret); // bit_key_finder - функція, з ідендифікатором __kernel в .сl файлі

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to create OpenCL kernel!\n");
                system("pause");
                return ret;
            }

            /* Відправка параметрів OpenCL Kernel*/
            ret = clSetKernelArg(kernel, 0, sizeof(cl_mem), (void*)&_devinText);
            ret = clSetKernelArg(kernel, 1, sizeof(cl_mem), (void*)&_devkey);
            ret = clSetKernelArg(kernel, 2, sizeof(cl_mem), (void*)&_devoutData);
            ret = clSetKernelArg(kernel, 3, sizeof(cl_mem), (void*)&_devcorrectDataTemp);


            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to Set Kernel Argumets!\n");
                system("pause");
                return ret;
            }

            /*Скопіюємо(cинхронно(3 параметр) ) дані на пристрій*/
            ret = clEnqueueWriteBuffer(command_queue, _devinText, CL_TRUE, 0, sizeof(unsigned char) * 7, cryptText, 0, NULL, NULL);
            ret = clEnqueueWriteBuffer(command_queue, _devcorrectDataTemp, CL_TRUE, 0, sizeof(unsigned char) * 7, inText, 0, NULL, NULL);

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to Write Buffer to Kernel!\n");
                system("pause");
                return ret;
            }

            do
            {
                ret = clSetKernelArg(kernel, 4, sizeof(size_t), (void*)&offset);
                ret = clEnqueueNDRangeKernel(command_queue, kernel, 1, NULL, &szGlobalWorkSize, &GroupWorkSize/*NULL*/, 0, NULL, NULL); // є ще такий варіант виконання ядра(, . . . общее количество work-item'ов, которые будут выполняться, розмір групи(тут драйвер вирішить сам), Следующие два парметра используются для синхронизации при использовании out-of-order исполнения команд. Это список событий, которые должны завершиться перед запуском этой команды (сначала идет размер списка, потом сам список).Через последний параметр возвращается объект - событие, сигнализирующее о завершении команды)
                ret = clEnqueueReadBuffer(command_queue, _devkey, CL_TRUE, 0, sizeof(unsigned char) * 10, key, 0, NULL, NULL);
                if (ret != CL_SUCCESS)
                {
                    printf("Error: Kernel Error!\n");
                    system("pause");
                    return ret;
                }
                offset++;
            } while (key[9] != 1); // 10-й байт ключа - флажок!

            float end_time = clock(); // кінцевий час
            float search_time = end_time - start_time; // час на виконання
            printf("Time: %.2f milliseconds\n", search_time);

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to Launch Kernel!\n");
                system("pause");
                return ret;
            }

            /*Зчитаємо результат з пристрою (тут синхронно(CL_TRUE))*/
            //ret = clEnqueueReadBuffer(command_queue, _devoutData, CL_TRUE, 0, sizeof(unsigned char) * 7, outData, 0, NULL, NULL);

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to Read Bufefr from Kernel!\n");
                system("pause");
                return ret;
            }

            printf("Key: |");
            for (int i = 0; i < 9; i++)
            {
                cout << dec << (int)key[i];
                printf("|");
            }
            printf("\n");

            /* Вивільняємо ресурси */
            ret = clFlush(command_queue);
            ret = clFinish(command_queue);
            ret = clReleaseKernel(kernel);
            ret = clReleaseProgram(program);
            ret = clReleaseMemObject(_devinText);
            ret = clReleaseMemObject(_devkey);
            ret = clReleaseMemObject(_devoutData);
            ret = clReleaseMemObject(_devcorrectDataTemp);
            ret = clReleaseCommandQueue(command_queue);
            ret = clReleaseContext(context);
            free(source_str);

            if (ret != CL_SUCCESS)
            {
                printf("Error: Failed to Free!\n");
                return ret;
            }

            system("pause");
        }
    }
}
