using System;
using System.IO;
using ReactUI.Jsx;
using Xunit;
using Xunit.Abstractions;

namespace ReactUI.Tests;

public class JsxDebugTest
{
    private readonly ITestOutputHelper _output;
    public JsxDebugTest(ITestOutputHelper output) => _output = output;

    [Fact]
    public void DebugCounterTransform()
    {
        var source = File.ReadAllText(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
                "ReactUI.Example", "ui", "Counter.jsx"));

        var transformed = JsxTransformer.Transform(source);
        var prepared = JintBridge.PrepareSource(transformed);

        _output.WriteLine("=== TRANSFORMED ===");
        _output.WriteLine(prepared);
    }

    [Fact]
    public void DebugSimpleTransform()
    {
        var source = @"
function Hello() {
  return (
    <div style={{ padding: 20 }}>
      <div>Hello World</div>
    </div>
  );
}

export default Hello;
";
        var transformed = JsxTransformer.Transform(source);
        var prepared = JintBridge.PrepareSource(transformed);

        _output.WriteLine("=== TRANSFORMED ===");
        _output.WriteLine(prepared);
    }
}
