using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("İstifadəçi adı və ya şifrə səhvdir.")
        {
        }
        public InvalidCredentialsException(string message, Exception innerException) : base(message, innerException)
        {
        }


        public InvalidCredentialsException(string message) : base(message)
        {
        }
    }
}
