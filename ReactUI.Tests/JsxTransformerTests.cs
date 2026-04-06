using ReactUI.Jsx;
using Xunit;

namespace ReactUI.Tests;

public class JsxTransformerTests
{
    // ──────────────────────────────────────────────
    //  Basic elements
    // ──────────────────────────────────────────────

    [Fact]
    public void SelfClosingTag_NoProps()
    {
        var result = JsxTransformer.Transform("var x = <div />;");
        Assert.Contains("createElement('div', null)", result);
    }

    [Fact]
    public void SelfClosingTag_WithStringProp()
    {
        var result = JsxTransformer.Transform("var x = <input placeholder=\"hello\" />;");
        Assert.Contains("createElement('input',", result);
        Assert.Contains("placeholder: 'hello'", result);
    }

    [Fact]
    public void OpenCloseTag_TextChild()
    {
        var result = JsxTransformer.Transform("var x = <div>Hello</div>;");
        Assert.Contains("createElement('div', null, 'Hello')", result);
    }

    [Fact]
    public void OpenCloseTag_ExpressionChild()
    {
        var result = JsxTransformer.Transform("var x = <div>{count}</div>;");
        Assert.Contains("createElement('div', null, count)", result);
    }

    // ──────────────────────────────────────────────
    //  Props
    // ──────────────────────────────────────────────

    [Fact]
    public void ExpressionProp()
    {
        var result = JsxTransformer.Transform("var x = <div style={{padding: 20}} />;");
        Assert.Contains("style: {padding: 20}", result);
    }

    [Fact]
    public void ArrowFunctionProp()
    {
        var result = JsxTransformer.Transform("var x = <button onClick={() => setCount(count + 1)} />;");
        Assert.Contains("onClick: () => setCount(count + 1)", result);
    }

    [Fact]
    public void BooleanShorthandProp()
    {
        var result = JsxTransformer.Transform("var x = <input disabled />;");
        Assert.Contains("disabled: true", result);
    }

    [Fact]
    public void MultipleProps()
    {
        var result = JsxTransformer.Transform("var x = <input value={name} placeholder=\"hi\" />;");
        Assert.Contains("value: name", result);
        Assert.Contains("placeholder: 'hi'", result);
    }

    // ──────────────────────────────────────────────
    //  Children
    // ──────────────────────────────────────────────

    [Fact]
    public void NestedElements()
    {
        var result = JsxTransformer.Transform("var x = <div><span>hi</span></div>;");
        Assert.Contains("createElement('div', null, createElement('span', null, 'hi'))", result);
    }

    [Fact]
    public void MultipleChildren()
    {
        var result = JsxTransformer.Transform("var x = <div><span /><span /></div>;");
        Assert.Contains("createElement('div', null, createElement('span', null), createElement('span', null))", result);
    }

    [Fact]
    public void MixedTextAndElements()
    {
        var result = JsxTransformer.Transform("var x = <div>Hello <span>world</span></div>;");
        Assert.Contains("'Hello'", result);
        Assert.Contains("createElement('span', null, 'world')", result);
    }

    [Fact]
    public void ExpressionAndElementChildren()
    {
        var result = JsxTransformer.Transform("var x = <div>{count}<span>!</span></div>;");
        Assert.Contains("count", result);
        Assert.Contains("createElement('span', null, '!')", result);
    }

    // ──────────────────────────────────────────────
    //  Conditional rendering
    // ──────────────────────────────────────────────

    [Fact]
    public void ConditionalAndOperator()
    {
        var result = JsxTransformer.Transform("var x = <div>{show && <span>yes</span>}</div>;");
        Assert.Contains("show && createElement('span', null, 'yes')", result);
    }

    [Fact]
    public void TernaryInExpression()
    {
        var result = JsxTransformer.Transform("var x = <div>{ok ? <span>yes</span> : <span>no</span>}</div>;");
        Assert.Contains("ok ? createElement('span', null, 'yes') : createElement('span', null, 'no')", result);
    }

    // ──────────────────────────────────────────────
    //  Fragments
    // ──────────────────────────────────────────────

    [Fact]
    public void Fragment_EmptyShorthand()
    {
        var result = JsxTransformer.Transform("var x = <><div /><span /></>;");
        Assert.Contains("createElement(Fragment, null", result);
    }

    // ──────────────────────────────────────────────
    //  Component tags (uppercase)
    // ──────────────────────────────────────────────

    [Fact]
    public void ComponentTag_PassedAsIdentifier()
    {
        var result = JsxTransformer.Transform("var x = <MyComponent foo={bar} />;");
        Assert.Contains("createElement(MyComponent, {foo: bar})", result);
    }

    [Fact]
    public void HtmlTag_PassedAsString()
    {
        var result = JsxTransformer.Transform("var x = <div />;");
        Assert.Contains("createElement('div'", result);
    }

    // ──────────────────────────────────────────────
    //  Preserves non-JSX code
    // ──────────────────────────────────────────────

    [Fact]
    public void PreservesPlainJs()
    {
        var input = "const a = 1 + 2;\nconst b = a < 3;";
        var result = JsxTransformer.Transform(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void PreservesStringLiterals()
    {
        var input = "const s = '<div>not jsx</div>';";
        var result = JsxTransformer.Transform(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void PreservesComments()
    {
        var input = "// <div>not jsx</div>\nconst x = 1;";
        var result = JsxTransformer.Transform(input);
        Assert.Contains("// <div>not jsx</div>", result);
    }

    // ──────────────────────────────────────────────
    //  .map() pattern
    // ──────────────────────────────────────────────

    [Fact]
    public void MapWithJsx()
    {
        var input = "var x = <div>{items.map(i => <span key={i}>{i}</span>)}</div>;";
        var result = JsxTransformer.Transform(input);
        Assert.Contains("items.map(i => createElement('span', {key: i}, i))", result);
    }

    // ──────────────────────────────────────────────
    //  Return statement context
    // ──────────────────────────────────────────────

    [Fact]
    public void ReturnJsx()
    {
        var input = "function Foo() { return <div>hi</div>; }";
        var result = JsxTransformer.Transform(input);
        Assert.Contains("return createElement('div', null, 'hi')", result);
    }

    // ──────────────────────────────────────────────
    //  Complete component
    // ──────────────────────────────────────────────

    [Fact]
    public void FullComponent_TransformsCorrectly()
    {
        var input = @"
function Counter() {
  const [count, setCount] = useState(0);
  return (
    <div style={{padding: 20}}>
      <div>{count}</div>
      <button onClick={() => setCount(count + 1)}>+1</button>
    </div>
  );
}";
        var result = JsxTransformer.Transform(input);

        // Should have createElement calls
        Assert.Contains("createElement('div',", result);
        Assert.Contains("createElement('button',", result);
        Assert.Contains("onClick: () => setCount(count + 1)", result);
        Assert.Contains("'+1'", result);

        // Should preserve hooks
        Assert.Contains("useState(0)", result);

        // Should NOT contain JSX angle brackets in output
        Assert.DoesNotContain("<div", result);
        Assert.DoesNotContain("<button", result);
        Assert.DoesNotContain("</div>", result);
    }

    // ──────────────────────────────────────────────
    //  Style with hover pseudo-state
    // ──────────────────────────────────────────────

    [Fact]
    public void StyleWithNestedHover()
    {
        var input = "var x = <div style={{background: '#fff', hover: {opacity: 0.8}}} />;";
        var result = JsxTransformer.Transform(input);
        Assert.Contains("hover: {opacity: 0.8}", result);
    }
}
