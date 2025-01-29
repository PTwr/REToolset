using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class DataBufferTests
    {
        [Fact]
        public void BasicTests()
        {
            var initialData = Enumerable.Range(0, 16).Select(x=>(byte)x).ToArray();

            IDataBuffer dataSlice = new DefaultDataBuffer(initialData, true);

            Assert.Equal(16, dataSlice.Length);

            var slice = dataSlice.AsSpan(5, 3);
            Assert.Equal(Enumerable.Range(5, 3).Select(x => (byte)x).ToArray(), slice.ToArray());

            //should resize data buffer
            slice = dataSlice.AsSpan(15, 4);
            Assert.Equal(16+3, dataSlice.Length);

            //resize should not screw up existing data
            Assert.Equal(15, dataSlice.ElementAt(15));
            //and managed memory should zero acquired space
            Assert.Equal(0, dataSlice.ElementAt(16));
            Assert.Equal(0, dataSlice.ElementAt(17));
            Assert.Equal(0, dataSlice.ElementAt(18));
        }
    }
}
