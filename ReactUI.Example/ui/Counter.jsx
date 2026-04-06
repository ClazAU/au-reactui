function Counter() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState('');

  useEffect(() => {
    log('Count changed to ' + count);
  }, [count]);

  return (
    <div className="panel" style={{ position: 'absolute', inset: [40, null, null, 40], width: 420 }}>
      <div className="title">ReactUI Counter</div>

      <div className="section">
        <div className="count" style={{ color: count >= 0 ? '#7c3aed' : '#ef4444' }}>
          {'Count: ' + count}
        </div>
        <div className="btn-row">
          <button className="btn btn-red" onClick={() => setCount(count - 5)}>- 5</button>
          <button className="btn btn-orange" onClick={() => setCount(count - 1)}>- 1</button>
          <button className="btn btn-gray" onClick={() => setCount(0)}>Reset</button>
          <button className="btn btn-green" onClick={() => setCount(count + 1)}>+ 1</button>
          <button className="btn btn-blue" onClick={() => setCount(count + 5)}>+ 5</button>
        </div>
      </div>

      <div className="section">
        <div className="label">Text Input:</div>
        <input
          className="input"
          value={name}
          onChange={setName}
          placeholder="Type something..."
        />
        {name.length > 0 && (
          <div className="greeting">{'Hello, ' + name + '!'}</div>
        )}
      </div>
    </div>
  );
}

export default Counter;
