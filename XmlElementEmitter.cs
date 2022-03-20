public sealed class XmlElementEmitterSettings {
    public string TabSequence { get; set; } = "    ";
    public string NewLine { get; set; } = Environment.NewLine;
}

public sealed class XmlElementEmitter {
    private enum XmlElementKind { Inline, Block, Meta }
    private struct XmlElement {
        public string Name { get; }
        public XmlElementKind Kind { get; }
        public Guid Id { get; }

        public XmlElement(string name, XmlElementKind kind) {
            Name = name;
            Kind = kind;
            Id = Guid.NewGuid();
        }
    }

    private readonly Action<string> _write;
    private readonly XmlElementEmitterSettings _settings;
    private readonly Stack<XmlElement> _elementStack;

    private int _tabs = 0;
    private bool _isNewLine = true;

    public XmlElementEmitterSettings Settings => _settings;

    public XmlElementEmitter(Action<string> write, Action<XmlElementEmitterSettings>? configure = null) {
        _write = write;
        _settings = new XmlElementEmitterSettings();
        _elementStack = new Stack<XmlElement>();

        if (configure is not null) {
            configure(_settings);
        }
    }
    
    // Element stack operations
    private Guid Push(string name, XmlElementKind kind) {
        var element = new XmlElement(name, kind);
        _elementStack.Push(element);
        return element.Id;
    }
    private XmlElement Pop(Guid id) {
        var element = _elementStack.Pop();
        if (element.Id != id) {
            throw new InvalidOperationException("Invalid element.");
        }
        return element;
    }

    // Text operations
    private void Write(string text) {
        if (_isNewLine) {
            for(int i = 0; i < _tabs; i++) {
                _write(_settings.TabSequence);
            }
            _isNewLine = false;
        }
        _write(text);
    }
    private void NewLine(int tabDelta = 0) {
        if (!_isNewLine) {
            _write(Settings.NewLine);
            _isNewLine = true;
        }
        _tabs += tabDelta;
    }

    // Element text operations
    private void OpenInline(string name, string? properties) {
        if(properties is null) {
            Write($"<{name}>");
        } else {
            Write($"<{name} {properties}>");
        }
    }
    private void CloseInline(string name) {
        Write($"</{name}>");
    }

    /// <summary>
    /// <code>
    /// Outer content&lt;name properties&gt;Outer content
    /// </code>
    /// </summary>
    public void InlineEmpty(string name, string? properties = null) {
        OpenInline(name, properties);
    }
    /// <summary>
    /// <code>
    /// Outer content
    /// &lt;name properties&gt;
    /// Outer content
    /// </code>
    /// </summary>
    public void MetaEmpty(string name, string? properties = null) {
        NewLine();
        OpenInline(name, properties);
        NewLine();
    }
    /// <summary>
    /// <code>
    /// Outer content&lt;name properties&gt;Inner content&lt;/name&gt;Outer content
    /// </code>
    /// </summary>
    public XmlElementHandle Inline(string name, string? properties = null) {
        var id = Push(name, XmlElementKind.Inline);
        OpenInline(name, properties);
        return new XmlElementHandle(this, id);
    }
    /// <summary>
    /// <code>
    /// Outer content
    /// &lt;name properties&gt;Inner content&lt;/name&gt;
    /// Outer content
    /// </code>
    /// </summary>
    public XmlElementHandle Meta(string name, string? properties = null) {
        var id = Push(name, XmlElementKind.Meta);
        NewLine();
        OpenInline(name, properties);
        return new XmlElementHandle(this, id);
    }
    /// <summary>
    /// <code>
    /// Outer content
    /// &lt;name properties&gt;
    ///     Inner content
    /// &lt;/name&gt;
    /// Outer content
    /// </code>
    /// </summary>
    public XmlElementHandle Block(string name, string? properties = null) {
        var id = Push(name, XmlElementKind.Block);
        NewLine();
        OpenInline(name, properties);
        NewLine(1);
        return new XmlElementHandle(this, id);
    }

    public void Text(string text) {
        if (text.Contains(Settings.NewLine)) {
            bool isFirst = true;
            foreach (var line in text.Split(Settings.NewLine)) {
                if (!isFirst) {
                    NewLine();
                } else {
                    isFirst = false;
                }
                Write(line);
            }
        } else {
            Write(text);
        }
    }

    public void Close(Guid id) {
        var element = Pop(id);
        switch (element.Kind) {
            case XmlElementKind.Inline:
                CloseInline(element.Name);
                break;
            case XmlElementKind.Meta:
                CloseInline(element.Name);
                NewLine();
                break;
            case XmlElementKind.Block:
                NewLine(-1);
                CloseInline(element.Name);
                NewLine();
                break;
        }
    }
}

public ref struct XmlElementHandle {
    private XmlElementEmitter _emitter;
    private Guid _id;

    public XmlElementHandle(XmlElementEmitter emitter, Guid id) {
        _emitter = emitter;
        _id = id;
    }

    public void Dispose() {
        _emitter.Close(_id);
    }
}