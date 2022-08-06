﻿
namespace RECV_Editor
{
    static class Constants
    {
        /// <summary>
        /// Names of the files inside the RDX file, extracted from the GC version since the PS2 version lacks names.
        /// </summary>
        public static readonly string[][] PS2_RDXFileNames = new string[][]
        {
            new string[]
            {
                "rm_0000.rdx",
                "rm_0010.rdx",
                "rm_0020.rdx",
                "rm_0021.rdx",
                "rm_0030.rdx",
                "rm_0031.rdx",
                "rm_0040.rdx",
                "rm_0050.rdx",
                "rm_0060.rdx",
                "rm_0070.rdx",
                "rm_0080.rdx",
                "rm_0090.rdx",
                "rm_0100.rdx",
                "rm_0110.rdx",
                "rm_0120.rdx",
                "rm_0130.rdx",
                "rm_0140.rdx",
                "rm_0150.rdx",
                "rm_0160.rdx",
                "rm_1000.rdx",
                "rm_1001.rdx",
                "rm_1002.rdx",
                "rm_1020.rdx",
                "rm_1021.rdx",
                "rm_1030.rdx",
                "rm_1040.rdx",
                "rm_1050.rdx",
                "rm_1060.rdx",
                "rm_1070.rdx",
                "rm_1080.rdx",
                "rm_1090.rdx",
                "rm_1100.rdx",
                "rm_1110.rdx",
                "rm_1120.rdx",
                "rm_1121.rdx",
                "rm_1122.rdx",
                "rm_1130.rdx",
                "rm_1140.rdx",
                "rm_2000.rdx",
                "rm_2010.rdx",
                "rm_2011.rdx",
                "rm_2020.rdx",
                "rm_2030.rdx",
                "rm_2031.rdx",
                "rm_2040.rdx",
                "rm_2050.rdx",
                "rm_2060.rdx",
                "rm_2070.rdx",
                "rm_3000.rdx",
                "rm_3010.rdx",
                "rm_3011.rdx",
                "rm_3020.rdx",
                "rm_3030.rdx",
                "rm_3040.rdx",
                "rm_3050.rdx",
                "rm_3060.rdx",
                "rm_3070.rdx",
                "rm_3080.rdx",
                "rm_3090.rdx",
                "rm_3091.rdx",
                "rm_3101.rdx",
                "rm_3110.rdx",
                "rm_3120.rdx",
                "rm_3130.rdx",
                "rm_3140.rdx",
                "rm_3150.rdx",
                "rm_3160.rdx",
                "rm_3170.rdx",
                "rm_3180.rdx",
                "rm_3190.rdx",
                "rm_3200.rdx",
                "rm_3210.rdx",
                "rm_3220.rdx",
                "rm_3230.rdx",
                "rm_3240.rdx",
                "rm_4000.rdx",
                "rm_4001.rdx",
                "rm_4010.rdx",
                "rm_4011.rdx",
                "rm_4012.rdx",
                "rm_4020.rdx",
                "rm_4030.rdx",
                "rm_4040.rdx",
                "rm_4050.rdx",
                "rm_5000.rdx",
                "rm_5001.rdx",
                "rm_5010.rdx",
                "rm_5500.rdx",
                "rm_5510.rdx",
                "rm_5520.rdx",
                "rm_5530.rdx",
                "rm_5540.rdx",
                "rm_5550.rdx",
                "rm_5580.rdx",
                "rm_5590.rdx",
                "rm_5600.rdx",
                "rm_5610.rdx",
                "rm_5620.rdx",
                "rm_5630.rdx",
                "rm_5640.rdx",
                "rm_5650.rdx",
                "rm_5660.rdx",
                "rm_5670.rdx",
                "rm_5680.rdx",
                "rm_5690.rdx",
                "rm_5700.rdx",
                "rm_5710.rdx",
                "rm_5800.rdx",
                "rm_5810.rdx",
                "rm_5820.rdx",
                "rm_5830.rdx",
                "rm_5840.rdx",
                "rm_6000.rdx",
                "rm_6010.rdx",
                "rm_6020.rdx",
                "rm_6030.rdx",
                "rm_6040.rdx",
                "rm_6050.rdx",
                "rm_6051.rdx",
                "rm_6060.rdx",
                "rm_6070.rdx",
                "rm_6080.rdx",
                "rm_6090.rdx",
                "rm_6100.rdx",
                "rm_7000.rdx",
                "rm_7010.rdx",
                "rm_7020.rdx",
                "rm_7030.rdx",
                "rm_7031.rdx",
                "rm_7040.rdx",
                "rm_7050.rdx",
                "rm_7060.rdx",
                "rm_7070.rdx",
                "rm_7071.rdx",
                "rm_7080.rdx",
                "rm_7081.rdx",
                "rm_7090.rdx",
                "rm_7100.rdx",
                "rm_7110.rdx",
                "rm_7120.rdx",
                "rm_7130.rdx",
                "rm_7140.rdx",
                "rm_7150.rdx",
                "rm_7160.rdx",
                "rm_7170.rdx",
                "rm_7180.rdx",
                "rm_7181.rdx",
                "rm_7190.rdx",
                "rm_7191.rdx",
                "rm_7200.rdx",
                "rm_7210.rdx",
                "rm_7220.rdx",
                "rm_7221.rdx",
                "rm_7230.rdx",
                "rm_7231.rdx",
                "rm_7240.rdx",
                "rm_7250.rdx",
                "rm_8000.rdx",
                "rm_8001.rdx",
                "rm_8010.rdx",
                "rm_8020.rdx",
                "rm_8030.rdx",
                "rm_8040.rdx",
                "rm_8050.rdx",
                "rm_9000.rdx",
                "rm_9010.rdx",
                "rm_9020.rdx",
                "rm_9030.rdx",
                "rm_9040.rdx",
                "rm_9050.rdx",
                "rm_9060.rdx",
                "rm_9070.rdx",
                "rm_9080.rdx",
                "rm_9090.rdx",
                "rm_9091.rdx",
                "rm_9100.rdx",
                "rm_9110.rdx",
                "rm_9120.rdx",
                "rm_9130.rdx",
                "rm_9140.rdx",
                "rm_9150.rdx",
                "rm_9160.rdx",
                "rm_9170.rdx",
                "rm_9180.rdx",
                "rm_9190.rdx",
                "rm_9200.rdx",
                "rm_9210.rdx",
                "rm_9220.rdx",
                "rm_9230.rdx",
                "rm_9240.rdx",
                "rm_9250.rdx",
                "rm_9260.rdx",
                "rm_9270.rdx",
                "rm_9280.rdx",
                "rm_9290.rdx",
                "rm_9300.rdx",
                "rm_9301.rdx",
                "rm_9302.rdx",
                "rm_9310.rdx",
                "rm_9320.rdx",
                "rm_9321.rdx",
                "rm_9340.rdx",
                "rm_9350.rdx",
                "rm_9360.rdx",
                "rm_9370.rdx"
            },

            new string[]
            {
                "r2_0000.rdx",
                "r2_0010.rdx",
                "r2_0020.rdx",
                "r2_0021.rdx",
                "r2_0030.rdx",
                "r2_0031.rdx",
                "r2_0040.rdx",
                "r2_0050.rdx",
                "r2_0060.rdx",
                "r2_0070.rdx",
                "r2_0080.rdx",
                "r2_0090.rdx",
                "r2_0100.rdx",
                "r2_0110.rdx",
                "r2_0120.rdx",
                "r2_0130.rdx",
                "r2_0140.rdx",
                "r2_0150.rdx",
                "r2_0160.rdx",
                "r2_1000.rdx",
                "r2_1001.rdx",
                "r2_1002.rdx",
                "r2_1020.rdx",
                "r2_1021.rdx",
                "r2_1030.rdx",
                "r2_1040.rdx",
                "r2_1050.rdx",
                "r2_1060.rdx",
                "r2_1070.rdx",
                "r2_1080.rdx",
                "r2_1090.rdx",
                "r2_1100.rdx",
                "r2_1110.rdx",
                "r2_1120.rdx",
                "r2_1121.rdx",
                "r2_1122.rdx",
                "r2_1130.rdx",
                "r2_1140.rdx",
                "r2_2000.rdx",
                "r2_2010.rdx",
                "r2_2011.rdx",
                "r2_2020.rdx",
                "r2_2030.rdx",
                "r2_2031.rdx",
                "r2_2040.rdx",
                "r2_2050.rdx",
                "r2_2060.rdx",
                "r2_2070.rdx",
                "r2_3000.rdx",
                "r2_3010.rdx",
                "r2_3011.rdx",
                "r2_3020.rdx",
                "r2_3030.rdx",
                "r2_3040.rdx",
                "r2_3050.rdx",
                "r2_3060.rdx",
                "r2_3070.rdx",
                "r2_3080.rdx",
                "r2_3090.rdx",
                "r2_3091.rdx",
                "r2_3101.rdx",
                "r2_3110.rdx",
                "r2_3120.rdx",
                "r2_3130.rdx",
                "r2_3140.rdx",
                "r2_3150.rdx",
                "r2_3160.rdx",
                "r2_3170.rdx",
                "r2_3180.rdx",
                "r2_3190.rdx",
                "r2_3200.rdx",
                "r2_3210.rdx",
                "r2_3220.rdx",
                "r2_3230.rdx",
                "r2_3240.rdx",
                "r2_4000.rdx",
                "r2_4001.rdx",
                "r2_4010.rdx",
                "r2_4011.rdx",
                "r2_4012.rdx",
                "r2_4020.rdx",
                "r2_4030.rdx",
                "r2_4040.rdx",
                "r2_4050.rdx",
                "r2_5000.rdx",
                "r2_5001.rdx",
                "r2_5010.rdx",
                "r2_5500.rdx",
                "r2_5510.rdx",
                "r2_5520.rdx",
                "r2_5530.rdx",
                "r2_5540.rdx",
                "r2_5550.rdx",
                "r2_5580.rdx",
                "r2_5590.rdx",
                "r2_5600.rdx",
                "r2_5610.rdx",
                "r2_5620.rdx",
                "r2_5630.rdx",
                "r2_5640.rdx",
                "r2_5650.rdx",
                "r2_5660.rdx",
                "r2_5670.rdx",
                "r2_5680.rdx",
                "r2_5690.rdx",
                "r2_5700.rdx",
                "r2_5710.rdx",
                "r2_5800.rdx",
                "r2_5810.rdx",
                "r2_5820.rdx",
                "r2_5830.rdx",
                "r2_5840.rdx",
                "r2_6000.rdx",
                "r2_6010.rdx",
                "r2_6020.rdx",
                "r2_6030.rdx",
                "r2_6040.rdx",
                "r2_6050.rdx",
                "r2_6051.rdx",
                "r2_6060.rdx",
                "r2_6070.rdx",
                "r2_6080.rdx",
                "r2_6090.rdx",
                "r2_6100.rdx",
                "r2_7000.rdx",
                "r2_7010.rdx",
                "r2_7020.rdx",
                "r2_7030.rdx",
                "r2_7031.rdx",
                "r2_7040.rdx",
                "r2_7050.rdx",
                "r2_7060.rdx",
                "r2_7070.rdx",
                "r2_7071.rdx",
                "r2_7080.rdx",
                "r2_7081.rdx",
                "r2_7090.rdx",
                "r2_7100.rdx",
                "r2_7110.rdx",
                "r2_7120.rdx",
                "r2_7130.rdx",
                "r2_7140.rdx",
                "r2_7150.rdx",
                "r2_7160.rdx",
                "r2_7170.rdx",
                "r2_7180.rdx",
                "r2_7181.rdx",
                "r2_7190.rdx",
                "r2_7191.rdx",
                "r2_7200.rdx",
                "r2_7210.rdx",
                "r2_7220.rdx",
                "r2_7221.rdx",
                "r2_7230.rdx",
                "r2_7231.rdx",
                "r2_7240.rdx",
                "r2_7250.rdx",
                "r2_8000.rdx",
                "r2_8001.rdx",
                "r2_8010.rdx",
                "r2_8020.rdx",
                "r2_8030.rdx",
                "r2_8040.rdx",
                "r2_8050.rdx",
                "r2_9000.rdx",
                "r2_9010.rdx",
                "r2_9020.rdx",
                "r2_9030.rdx",
                "r2_9040.rdx",
                "r2_9050.rdx",
                "r2_9060.rdx",
                "r2_9070.rdx",
                "r2_9080.rdx",
                "r2_9090.rdx",
                "r2_9091.rdx",
                "r2_9100.rdx",
                "r2_9110.rdx",
                "r2_9120.rdx",
                "r2_9130.rdx",
                "r2_9140.rdx",
                "r2_9150.rdx",
                "r2_9160.rdx",
                "r2_9170.rdx",
                "r2_9180.rdx",
                "r2_9190.rdx",
                "r2_9200.rdx",
                "r2_9210.rdx",
                "r2_9220.rdx",
                "r2_9230.rdx",
                "r2_9240.rdx",
                "r2_9250.rdx",
                "r2_9260.rdx",
                "r2_9270.rdx",
                "r2_9280.rdx",
                "r2_9290.rdx",
                "r2_9300.rdx",
                "r2_9301.rdx",
                "r2_9302.rdx",
                "r2_9310.rdx",
                "r2_9320.rdx",
                "r2_9321.rdx",
                "r2_9340.rdx",
                "r2_9350.rdx",
                "r2_9360.rdx",
                "r2_9370.rdx"
            },

            new string[]
            {
                "r5_0000.rdx",
                "r5_0010.rdx",
                "r5_0020.rdx",
                "r5_0021.rdx",
                "r5_0030.rdx",
                "r5_0031.rdx",
                "r5_0040.rdx",
                "r5_0050.rdx",
                "r5_0060.rdx",
                "r5_0070.rdx",
                "r5_0080.rdx",
                "r5_0090.rdx",
                "r5_0100.rdx",
                "r5_0110.rdx",
                "r5_0120.rdx",
                "r5_0130.rdx",
                "r5_0140.rdx",
                "r5_0150.rdx",
                "r5_0160.rdx",
                "r5_1000.rdx",
                "r5_1001.rdx",
                "r5_1002.rdx",
                "r5_1020.rdx",
                "r5_1021.rdx",
                "r5_1030.rdx",
                "r5_1040.rdx",
                "r5_1050.rdx",
                "r5_1060.rdx",
                "r5_1070.rdx",
                "r5_1080.rdx",
                "r5_1090.rdx",
                "r5_1100.rdx",
                "r5_1110.rdx",
                "r5_1120.rdx",
                "r5_1121.rdx",
                "r5_1122.rdx",
                "r5_1130.rdx",
                "r5_1140.rdx",
                "r5_2000.rdx",
                "r5_2010.rdx",
                "r5_2011.rdx",
                "r5_2020.rdx",
                "r5_2030.rdx",
                "r5_2031.rdx",
                "r5_2040.rdx",
                "r5_2050.rdx",
                "r5_2060.rdx",
                "r5_2070.rdx",
                "r5_3000.rdx",
                "r5_3010.rdx",
                "r5_3011.rdx",
                "r5_3020.rdx",
                "r5_3030.rdx",
                "r5_3040.rdx",
                "r5_3050.rdx",
                "r5_3060.rdx",
                "r5_3070.rdx",
                "r5_3080.rdx",
                "r5_3090.rdx",
                "r5_3091.rdx",
                "r5_3101.rdx",
                "r5_3110.rdx",
                "r5_3120.rdx",
                "r5_3130.rdx",
                "r5_3140.rdx",
                "r5_3150.rdx",
                "r5_3160.rdx",
                "r5_3170.rdx",
                "r5_3180.rdx",
                "r5_3190.rdx",
                "r5_3200.rdx",
                "r5_3210.rdx",
                "r5_3220.rdx",
                "r5_3230.rdx",
                "r5_3240.rdx",
                "r5_4000.rdx",
                "r5_4001.rdx",
                "r5_4010.rdx",
                "r5_4011.rdx",
                "r5_4012.rdx",
                "r5_4020.rdx",
                "r5_4030.rdx",
                "r5_4040.rdx",
                "r5_4050.rdx",
                "r5_5000.rdx",
                "r5_5001.rdx",
                "r5_5010.rdx",
                "r5_5500.rdx",
                "r5_5510.rdx",
                "r5_5520.rdx",
                "r5_5530.rdx",
                "r5_5540.rdx",
                "r5_5550.rdx",
                "r5_5580.rdx",
                "r5_5590.rdx",
                "r5_5600.rdx",
                "r5_5610.rdx",
                "r5_5620.rdx",
                "r5_5630.rdx",
                "r5_5640.rdx",
                "r5_5650.rdx",
                "r5_5660.rdx",
                "r5_5670.rdx",
                "r5_5680.rdx",
                "r5_5690.rdx",
                "r5_5700.rdx",
                "r5_5710.rdx",
                "r5_5800.rdx",
                "r5_5810.rdx",
                "r5_5820.rdx",
                "r5_5830.rdx",
                "r5_5840.rdx",
                "r5_6000.rdx",
                "r5_6010.rdx",
                "r5_6020.rdx",
                "r5_6030.rdx",
                "r5_6040.rdx",
                "r5_6050.rdx",
                "r5_6051.rdx",
                "r5_6060.rdx",
                "r5_6070.rdx",
                "r5_6080.rdx",
                "r5_6090.rdx",
                "r5_6100.rdx",
                "r5_7000.rdx",
                "r5_7010.rdx",
                "r5_7020.rdx",
                "r5_7030.rdx",
                "r5_7031.rdx",
                "r5_7040.rdx",
                "r5_7050.rdx",
                "r5_7060.rdx",
                "r5_7070.rdx",
                "r5_7071.rdx",
                "r5_7080.rdx",
                "r5_7081.rdx",
                "r5_7090.rdx",
                "r5_7100.rdx",
                "r5_7110.rdx",
                "r5_7120.rdx",
                "r5_7130.rdx",
                "r5_7140.rdx",
                "r5_7150.rdx",
                "r5_7160.rdx",
                "r5_7170.rdx",
                "r5_7180.rdx",
                "r5_7181.rdx",
                "r5_7190.rdx",
                "r5_7191.rdx",
                "r5_7200.rdx",
                "r5_7210.rdx",
                "r5_7220.rdx",
                "r5_7221.rdx",
                "r5_7230.rdx",
                "r5_7231.rdx",
                "r5_7240.rdx",
                "r5_7250.rdx",
                "r5_8000.rdx",
                "r5_8001.rdx",
                "r5_8010.rdx",
                "r5_8020.rdx",
                "r5_8030.rdx",
                "r5_8040.rdx",
                "r5_8050.rdx",
                "r5_9000.rdx",
                "r5_9010.rdx",
                "r5_9020.rdx",
                "r5_9030.rdx",
                "r5_9040.rdx",
                "r5_9050.rdx",
                "r5_9060.rdx",
                "r5_9070.rdx",
                "r5_9080.rdx",
                "r5_9090.rdx",
                "r5_9091.rdx",
                "r5_9100.rdx",
                "r5_9110.rdx",
                "r5_9120.rdx",
                "r5_9130.rdx",
                "r5_9140.rdx",
                "r5_9150.rdx",
                "r5_9160.rdx",
                "r5_9170.rdx",
                "r5_9180.rdx",
                "r5_9190.rdx",
                "r5_9200.rdx",
                "r5_9210.rdx",
                "r5_9220.rdx",
                "r5_9230.rdx",
                "r5_9240.rdx",
                "r5_9250.rdx",
                "r5_9260.rdx",
                "r5_9270.rdx",
                "r5_9280.rdx",
                "r5_9290.rdx",
                "r5_9300.rdx",
                "r5_9301.rdx",
                "r5_9302.rdx",
                "r5_9310.rdx",
                "r5_9320.rdx",
                "r5_9321.rdx",
                "r5_9340.rdx",
                "r5_9350.rdx",
                "r5_9360.rdx",
                "r5_9370.rdx"
            },

            new string[]
            {
                "r4_0000.rdx",
                "r4_0010.rdx",
                "r4_0020.rdx",
                "r4_0021.rdx",
                "r4_0030.rdx",
                "r4_0031.rdx",
                "r4_0040.rdx",
                "r4_0050.rdx",
                "r4_0060.rdx",
                "r4_0070.rdx",
                "r4_0080.rdx",
                "r4_0090.rdx",
                "r4_0100.rdx",
                "r4_0110.rdx",
                "r4_0120.rdx",
                "r4_0130.rdx",
                "r4_0140.rdx",
                "r4_0150.rdx",
                "r4_0160.rdx",
                "r4_1000.rdx",
                "r4_1001.rdx",
                "r4_1002.rdx",
                "r4_1020.rdx",
                "r4_1021.rdx",
                "r4_1030.rdx",
                "r4_1040.rdx",
                "r4_1050.rdx",
                "r4_1060.rdx",
                "r4_1070.rdx",
                "r4_1080.rdx",
                "r4_1090.rdx",
                "r4_1100.rdx",
                "r4_1110.rdx",
                "r4_1120.rdx",
                "r4_1121.rdx",
                "r4_1122.rdx",
                "r4_1130.rdx",
                "r4_1140.rdx",
                "r4_2000.rdx",
                "r4_2010.rdx",
                "r4_2011.rdx",
                "r4_2020.rdx",
                "r4_2030.rdx",
                "r4_2031.rdx",
                "r4_2040.rdx",
                "r4_2050.rdx",
                "r4_2060.rdx",
                "r4_2070.rdx",
                "r4_3000.rdx",
                "r4_3010.rdx",
                "r4_3011.rdx",
                "r4_3020.rdx",
                "r4_3030.rdx",
                "r4_3040.rdx",
                "r4_3050.rdx",
                "r4_3060.rdx",
                "r4_3070.rdx",
                "r4_3080.rdx",
                "r4_3090.rdx",
                "r4_3091.rdx",
                "r4_3101.rdx",
                "r4_3110.rdx",
                "r4_3120.rdx",
                "r4_3130.rdx",
                "r4_3140.rdx",
                "r4_3150.rdx",
                "r4_3160.rdx",
                "r4_3170.rdx",
                "r4_3180.rdx",
                "r4_3190.rdx",
                "r4_3200.rdx",
                "r4_3210.rdx",
                "r4_3220.rdx",
                "r4_3230.rdx",
                "r4_3240.rdx",
                "r4_4000.rdx",
                "r4_4001.rdx",
                "r4_4010.rdx",
                "r4_4011.rdx",
                "r4_4012.rdx",
                "r4_4020.rdx",
                "r4_4030.rdx",
                "r4_4040.rdx",
                "r4_4050.rdx",
                "r4_5000.rdx",
                "r4_5001.rdx",
                "r4_5010.rdx",
                "r4_5500.rdx",
                "r4_5510.rdx",
                "r4_5520.rdx",
                "r4_5530.rdx",
                "r4_5540.rdx",
                "r4_5550.rdx",
                "r4_5580.rdx",
                "r4_5590.rdx",
                "r4_5600.rdx",
                "r4_5610.rdx",
                "r4_5620.rdx",
                "r4_5630.rdx",
                "r4_5640.rdx",
                "r4_5650.rdx",
                "r4_5660.rdx",
                "r4_5670.rdx",
                "r4_5680.rdx",
                "r4_5690.rdx",
                "r4_5700.rdx",
                "r4_5710.rdx",
                "r4_5800.rdx",
                "r4_5810.rdx",
                "r4_5820.rdx",
                "r4_5830.rdx",
                "r4_5840.rdx",
                "r4_6000.rdx",
                "r4_6010.rdx",
                "r4_6020.rdx",
                "r4_6030.rdx",
                "r4_6040.rdx",
                "r4_6050.rdx",
                "r4_6051.rdx",
                "r4_6060.rdx",
                "r4_6070.rdx",
                "r4_6080.rdx",
                "r4_6090.rdx",
                "r4_6100.rdx",
                "r4_7000.rdx",
                "r4_7010.rdx",
                "r4_7020.rdx",
                "r4_7030.rdx",
                "r4_7031.rdx",
                "r4_7040.rdx",
                "r4_7050.rdx",
                "r4_7060.rdx",
                "r4_7070.rdx",
                "r4_7071.rdx",
                "r4_7080.rdx",
                "r4_7081.rdx",
                "r4_7090.rdx",
                "r4_7100.rdx",
                "r4_7110.rdx",
                "r4_7120.rdx",
                "r4_7130.rdx",
                "r4_7140.rdx",
                "r4_7150.rdx",
                "r4_7160.rdx",
                "r4_7170.rdx",
                "r4_7180.rdx",
                "r4_7181.rdx",
                "r4_7190.rdx",
                "r4_7191.rdx",
                "r4_7200.rdx",
                "r4_7210.rdx",
                "r4_7220.rdx",
                "r4_7221.rdx",
                "r4_7230.rdx",
                "r4_7231.rdx",
                "r4_7240.rdx",
                "r4_7250.rdx",
                "r4_8000.rdx",
                "r4_8001.rdx",
                "r4_8010.rdx",
                "r4_8020.rdx",
                "r4_8030.rdx",
                "r4_8040.rdx",
                "r4_8050.rdx",
                "r4_9000.rdx",
                "r4_9010.rdx",
                "r4_9020.rdx",
                "r4_9030.rdx",
                "r4_9040.rdx",
                "r4_9050.rdx",
                "r4_9060.rdx",
                "r4_9070.rdx",
                "r4_9080.rdx",
                "r4_9090.rdx",
                "r4_9091.rdx",
                "r4_9100.rdx",
                "r4_9110.rdx",
                "r4_9120.rdx",
                "r4_9130.rdx",
                "r4_9140.rdx",
                "r4_9150.rdx",
                "r4_9160.rdx",
                "r4_9170.rdx",
                "r4_9180.rdx",
                "r4_9190.rdx",
                "r4_9200.rdx",
                "r4_9210.rdx",
                "r4_9220.rdx",
                "r4_9230.rdx",
                "r4_9240.rdx",
                "r4_9250.rdx",
                "r4_9260.rdx",
                "r4_9270.rdx",
                "r4_9280.rdx",
                "r4_9290.rdx",
                "r4_9300.rdx",
                "r4_9301.rdx",
                "r4_9302.rdx",
                "r4_9310.rdx",
                "r4_9320.rdx",
                "r4_9321.rdx",
                "r4_9340.rdx",
                "r4_9350.rdx",
                "r4_9360.rdx",
                "r4_9370.rdx"
            }
        };

        /// <summary>
        /// Custom names for Dreamcast's ADV.AFS files.
        /// </summary>
        public static readonly string[] DC_AdvFileNames = new string[]
        {
            "intrologos.bin",
            "capcomlogo1.bin",
            "titlemenu.bin",
            "SND_residentevil.ADX",
            "optionmenu.bin",
            "discchange1.bin",
            "SND_deathClaire.ADX",
            "SND_deathSteve.ADX",
            "SND_deathWesker.ADX",
            "SND_deathChris.ADX",
            "SND_explosion1.ADX",
            "SND_explosion2.ADX",
            "SND_explosion3.ADX",
            "SND_explosion4.ADX",
            "SND_countdownStart.ADX",
            "SND_countdown10.ADX",
            "SND_countdown9.ADX",
            "SND_countdown8.ADX",
            "SND_countdown7.ADX",
            "SND_countdown6.ADX",
            "SND_countdown5.ADX",
            "SND_countdown4.ADX",
            "SND_countdown3.ADX",
            "SND_countdown2.ADX",
            "SND_countdown1.ADX",
            "SND_countdown0.ADX",
            "SND_countdownStart1.ADX",
            "SND_countdownStart2.ADX",
            "SND_countdownStart3.ADX",
            "SND_countdownStart4.ADX",
            "discchange2.bin"
        };
    }
}