function Counter() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState('');
  const drag = useDrag(40, 40);

  useEffect(() => {
    log('Count changed to ' + count);
  }, [count]);

  return (
    <div className="panel" style={{ position: 'absolute', inset: [drag.y, null, null, drag.x], width: 420, cursor: 'grab' }}>
      <div className="title">ReactUI Counter</div>
      <div className="subtitle">Hot-reloadable JSX using the same Shadcn classes</div>

      <div className="section">
        <div className="count" style={{ color: count >= 0 ? '#fafafa' : '#7f1d1d' }}>
          {'Count: ' + count}
        </div>
        <div className="btn-row">
          <button className="btn btn-sm btn-destructive" onClick={() => setCount(count - 5)}>- 5</button>
          <button className="btn btn-sm btn-outline" onClick={() => setCount(count - 1)}>- 1</button>
          <button className="btn btn-sm btn-secondary" onClick={() => setCount(0)}>Reset</button>
          <button className="btn btn-sm btn-outline" onClick={() => setCount(count + 1)}>+ 1</button>
          <button className="btn btn-sm" onClick={() => setCount(count + 5)}>+ 5</button>
        </div>
      </div>

      <div className="section">
        <div className="label">Your name</div>
        <input
          className="input"
          value={name}
          onChange={setName}
          placeholder="Type something..."
        />
        {name.length > 0 && (
          <div className="badge">
            <div className="greeting">{'Hello, ' + name + '!'}</div>
          </div>
        )}
      </div>
    </div>
  );
}

export default Counter;
