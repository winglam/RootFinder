using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RootFinder.Data
{
    public class ReturnVal
    {
        public enum Type { BoolType, IntType, StringType, Undefined, Inconsistent }

        public string Value;
        public Type ValueType;
        public ReturnVal(string value)
        {
            //ValueType = CheckBoolType(value);
            //if (ValueType != Type.BoolType)
            //{
            //    ValueType = CheckIntType(value);
            //}
            ValueType = Type.StringType; 

            Value = value;
        }

        public ReturnVal(string value, Type type) : this(value)
        {
            ValueType = type;
        }

        public override String ToString()
        {
            return ValueType == Type.StringType ? $"\"{Value}\"" : Value;
        }

        private Type CheckBoolType(String s)
        {
            try
            {
                Boolean.Parse(s);
                return Type.BoolType;
            }
            catch (Exception e)
            {
                return Type.StringType;
            }
        }

        private Type CheckIntType(String s)
        {
            try
            {
                Int32.Parse(s);
                return Type.IntType;
            }
            catch (Exception e)
            {
                return Type.StringType;
            }
        }
    }
}
