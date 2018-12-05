using System;
using System.Collections.Generic;
using System.Text;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls {
    public static class CommonNumberTransport {
        public enum NumberType: byte {
            Int64,
            Int32,
            Int16,

            Double,
            Single,

            UInt64,
            UInt32,
            UInt16,

            Byte,
            SByte,

            DateTime,

            //BigInt,
            //TimeSpan
        }

        public static (NumberType numberType, byte[] encoded) EncodeNumber(object number){
            Type t = number.GetType();
            NumberType numberType = Enum.Parse<NumberType>(t.Name);
            byte[] encoded = conversions[numberType].toBytes(number);
            return (numberType, encoded);
        }
        public static object DecodeNumber(this NumberType numberType, byte[] encoded){
            return conversions[numberType].fromBytes(encoded);
        }

        private static Dictionary<NumberType, (Func<object, byte[]> toBytes, Func<byte[], object> fromBytes)>
            conversions = new Dictionary<NumberType, (Func<object, byte[]> toBytes, Func<byte[], object> fromBytes)>() {
                {NumberType.Int64,
                    ((x) => {
                        var v = (long)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToInt64(x);
                    })
                },
                {NumberType.Int32,
                    ((x) => {
                        var v = (int)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToInt32(x);
                    })
                },
                {NumberType.Int16,
                    ((x) => {
                        var v = (short)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToInt16(x);
                    })
                },
                
                {NumberType.Double,
                    ((x) => {
                        var v = (double)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToDouble(x);
                    })
                },
                {NumberType.Single,
                    ((x) => {
                        var v = (float)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToSingle(x);
                    })
                },

                {NumberType.UInt64,
                    ((x) => {
                        var v = (ulong)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToUInt64(x);
                    })
                },
                {NumberType.UInt32,
                    ((x) => {
                        var v = (uint)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToUInt32(x);
                    })
                },
                {NumberType.UInt16,
                    ((x) => {
                        var v = (ushort)x;
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        return BitConverter.ToUInt16(x);
                    })
                },

                {NumberType.Byte,
                    ((x) => {
                        byte[] ret = new byte[]{
                            (byte)x
                        };
                        return ret;
                    },
                    (x) => {
                        return x[0];
                    })
                },
                {NumberType.SByte,
                    ((x) => {
                        unchecked{
                            byte[] ret = new byte[]{
                                (byte)x
                            };
                            return ret;
                        }
                    },
                    (x) => {
                        unchecked{
                            return (sbyte)x[0];
                        }
                    })
                },

                {NumberType.DateTime,
                    ((x) => {
                        DateTime dt = (DateTime)x;
                        long v = dt.ToBinary();
                        byte[] ret = BitConverter.GetBytes(v);
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(ret);
                        }
                        return ret;
                    },
                    (x) => {
                        if(BitConverter.IsLittleEndian){
                            Array.Reverse(x);
                        }
                        long l = BitConverter.ToInt64(x);
                        return DateTime.FromBinary(l);
                    })
                },
            };
    }
}
