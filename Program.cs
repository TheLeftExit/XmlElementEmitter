var emit = new XmlElementEmitter(x => Console.Write(x));

using (emit.Block("html")) {
    using (emit.Block("head")) {
        using (emit.Meta("title")) emit.Text("Home");
    }
    using (emit.Block("body")) {
        using (emit.Block("h1")) emit.Text("Welcome");
        using (emit.Block("p")) {
            emit.Text("Welcome to my ");
            using (emit.Inline("b")) emit.Text("website");
            emit.Text("!");
        }
        using(emit.Block("div", "style='font-family: Bahnschrift'")) {
            emit.Text("Line 1" + Environment.NewLine + "Still line 1");
            emit.MetaEmpty("br");
            emit.Text("Line 2");
        }
    }
}