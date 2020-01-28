using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Rules
{
    public class SocketRule : ICustomRule,IDisposable
    {
        public string Host { get; set; }
        public int Port { get; set; }
        
        private TcpClient _tcp;
        private NetworkStream _stream;
        
        public RuleAction Apply(string fieldName, string fieldValue, out FailureClassification classification,
            out int offset, out string badWord)
        {
            if (_stream == null)
            {
                _tcp = new TcpClient(Host, Port);
                _stream = _tcp.GetStream();
            }

            // Translate the passed message into ASCII and store it as a Byte array.
            byte[] data = Encoding.UTF8.GetBytes(fieldValue + "\0");

            // Send the message to the connected TcpServer. 
            _stream.Write(data, 0, data.Length);
   
            // Receive the TcpServer.response.
    
            // Buffer to store the response bytes.
            data = new byte[256];

            // String to store the response UTF8 representation.

            // Read the first batch of the TcpServer response bytes.
            int bytes = _stream.Read(data, 0, data.Length);
            var responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

            return HandleResponse(responseData, out classification, out offset, out badWord);
        }

        public RuleAction HandleResponse(string responseData, out FailureClassification classification, out int offset,
            out string badWord)
        {
            if (string.Equals(responseData, "\0"))
            {
                classification = FailureClassification.None;
                offset = -1;
                badWord = null;
                return RuleAction.None;
            }
                
            var result = responseData.Split("\0",StringSplitOptions.RemoveEmptyEntries);
            
            if(result.Length != 3)
                throw new Exception($"Unexpected number of tokens in response from TCP client (expected '{2}' but got '{result.Length}').  Full message was '{responseData}' (expected <classification><offset> or <null terminator>)");

            object c;
            if (!Enum.TryParse(typeof(FailureClassification), result[0],true, out c))
                throw new Exception($"Could not parse TCP client classification '{result[0]}' (expected a member of Enum FailureClassification)");
            classification = (FailureClassification)c;

            if(!int.TryParse(result[1],out offset))
                throw new Exception($"Failed to parse offset from TCP client response.  Response was '{result[1]}' (expected int)");

            badWord = result[2];

            return RuleAction.Report;
        }

        public void Dispose()
        {
            _tcp?.Dispose();
            _stream?.Dispose();
        }
    }
}
