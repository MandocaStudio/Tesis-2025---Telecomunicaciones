using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Lector del Marco Teórico (UI Toolkit), 100% data-driven.
/// - Contenido completo del capítulo (las citas [n] de la tesis se ocultan en pantalla).
/// - PAGINACIÓN FIJA: cada página se declara en <see cref="Layout"/> indicando qué secciones
///   van en la columna izquierda y cuáles en la derecha (formato libro).
/// - Una página sin columna derecha reparte la sección entre las dos columnas por peso.
/// - El cuerpo va JUSTIFICADO con flexbox (UI Toolkit no soporta -unity-text-align: justify).
/// - Figuras (imagen/diagrama/tabla) detrás de un BOTÓN que abre una ventana flotante,
///   para no romper la maqueta a dos columnas.
/// - Las imágenes se cargan de Assets/Resources/Teoria (no hace falta cablearlas en el Inspector).
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(100)]
public class TheoryReader : MonoBehaviour
{
    [SerializeField] private string backScene = "Menu Inicial";

    private const string FigResourceFolder = "Teoria/";

    // ───────────────────────── diagrama ─────────────────────────
    private class DItem
    {
        public enum K { Box, Acc, Comp, AR, AL, AD, AU, ABi, Gap }
        public K k; public string text; public string[] subs;
        public static DItem Box(string t)  => new DItem { k = K.Box, text = t };
        public static DItem Acc(string t)  => new DItem { k = K.Acc, text = t };
        public static DItem Comp(string title, params string[] subs) => new DItem { k = K.Comp, text = title, subs = subs };
        public static DItem AR()           => new DItem { k = K.AR };
        public static DItem AL()           => new DItem { k = K.AL };
        public static DItem AD(string t = null) => new DItem { k = K.AD, text = t };
        public static DItem AU(string t = null) => new DItem { k = K.AU, text = t };
        public static DItem ABi(string t = null) => new DItem { k = K.ABi, text = t };
        public static DItem Gap()          => new DItem { k = K.Gap };
    }

    private class DRow
    {
        public string label;
        public string align = "center";
        public List<DItem> items;
        public DRow(string lbl, params DItem[] it) { label = lbl; items = new List<DItem>(it); }
    }

    // ───────────────────────── bloques ─────────────────────────
    private enum BType { Head, Sub, Para, Bullet, Formula, Figure }
    private enum FKind { Image, Diagram, Table }

    private class Block
    {
        public BType t;
        public string text;
        public string cap;
        public FKind fkind;
        public string imageKey;
        public List<DRow> diag;
        public string[][] table;

        public static Block H(string s)   => new Block { t = BType.Head, text = s };
        public static Block S(string s)   => new Block { t = BType.Sub, text = s };
        public static Block P(string s)   => new Block { t = BType.Para, text = s };
        public static Block Bul(string s) => new Block { t = BType.Bullet, text = s };
        public static Block For(string s) => new Block { t = BType.Formula, text = s };
        public static Block Img(string cap, string key)
            => new Block { t = BType.Figure, fkind = FKind.Image, cap = cap, imageKey = key };
        public static Block Tab(string cap, string[][] tbl)
            => new Block { t = BType.Figure, fkind = FKind.Table, cap = cap, table = tbl };
        public static Block Diagram(string cap, params DRow[] rows)
            => new Block { t = BType.Figure, fkind = FKind.Diagram, cap = cap, diag = new List<DRow>(rows) };
    }

    private class Section { public string num; public string title; public List<Block> blocks; }

    /// <summary>Una página del libro. <c>right = null</c> reparte <c>left</c> entre ambas columnas.</summary>
    private class PageSpec { public string[] left; public string[] right; }

    // Maqueta pedida por el tutor: qué va a cada lado en cada página.
    private static readonly PageSpec[] Layout =
    {
        new PageSpec { left = new[] { "1.1" },          right = new[] { "1.1.1" } },
        new PageSpec { left = new[] { "1.2" },          right = new[] { "1.3" }   },
        new PageSpec { left = new[] { "1.4", "1.5" },   right = new[] { "1.5.1" } },
        new PageSpec { left = new[] { "1.5.2" },        right = new[] { "1.5.3" } },
        new PageSpec { left = new[] { "1.5.4" },        right = new[] { "1.5.5" } },
        new PageSpec { left = new[] { "1.6" },          right = new[] { "1.6.1" } },
        new PageSpec { left = new[] { "1.7" }  },
        new PageSpec { left = new[] { "1.8" }  },
        new PageSpec { left = new[] { "1.9" }  },
        new PageSpec { left = new[] { "1.10" } },
    };

    private List<Section> sections;
    private Dictionary<string, int> byNum;
    private List<PageSpec> pages;
    private int[] sectionToPage;
    private int current;

    private readonly Dictionary<string, Texture2D> figCache = new Dictionary<string, Texture2D>();

    private Label pageTitle, pageNum, navLabel, figModalTitle;
    private VisualElement pageContent, tocModal, figModal;
    private ScrollView contentScroll, tocList, figModalContent;
    private bool tocBuilt;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) { Debug.LogWarning("[TheoryReader] root no listo."); return; }

        pageTitle     = root.Q<Label>("PageTitle");
        pageNum       = root.Q<Label>("PageNum");
        navLabel      = root.Q<Label>("NavLabel");
        pageContent   = root.Q<VisualElement>("PageContent");
        contentScroll = root.Q<ScrollView>("ContentScroll");
        tocModal      = root.Q<VisualElement>("TocModal");
        tocList       = root.Q<ScrollView>("TocList");
        figModal        = root.Q<VisualElement>("FigModal");
        figModalContent = root.Q<ScrollView>("FigModalContent");
        figModalTitle   = root.Q<Label>("FigModalTitle");

        sections = BuildContent();
        BuildPages();
        current = 0;

        root.Q<Button>("BtnPrev").clicked   += () => ShowPage(current - 1);
        root.Q<Button>("BtnNext").clicked   += () => ShowPage(current + 1);
        root.Q<Button>("BtnIndice").clicked += OpenToc;
        root.Q<Button>("BtnTocClose").clicked += CloseToc;
        var btnFigClose = root.Q<Button>("BtnFigClose");
        if (btnFigClose != null) btnFigClose.clicked += CloseFig;

        if (tocModal != null) tocModal.RegisterCallback<ClickEvent>(e => { if (e.target == tocModal) CloseToc(); });
        if (figModal != null) figModal.RegisterCallback<ClickEvent>(e => { if (e.target == figModal) CloseFig(); });

        var back = root.Q<Button>("BtnBack");
        if (back != null && !string.IsNullOrEmpty(backScene))
            back.clicked += () => SceneManager.LoadScene(backScene);

        ShowPage(0);
    }

    // ───────────────────────── paginación ─────────────────────────
    private void BuildPages()
    {
        byNum = new Dictionary<string, int>();
        for (int i = 0; i < sections.Count; i++) byNum[sections[i].num] = i;

        pages = new List<PageSpec>();
        sectionToPage = new int[sections.Count];
        foreach (var spec in Layout)
        {
            int p = pages.Count;
            pages.Add(spec);
            foreach (var num in SectionsOf(spec))
                if (byNum.TryGetValue(num, out int si)) sectionToPage[si] = p;
                else Debug.LogWarning("[TheoryReader] la maqueta referencia una sección inexistente: " + num);
        }
    }

    private static IEnumerable<string> SectionsOf(PageSpec spec)
    {
        foreach (var n in spec.left) yield return n;
        if (spec.right != null) foreach (var n in spec.right) yield return n;
    }

    private void ShowPage(int p)
    {
        if (pages == null || pages.Count == 0) return;
        current = Mathf.Clamp(p, 0, pages.Count - 1);
        var spec = pages[current];

        var nums = new List<string>(SectionsOf(spec));
        var first = sections[byNum[nums[0]]];
        var last  = sections[byNum[nums[nums.Count - 1]]];
        if (pageTitle != null)
            pageTitle.text = nums.Count == 1 ? first.num + "   " + first.title
                                             : "Marco Teórico  ·  " + first.num + " – " + last.num;
        if (pageNum != null)  pageNum.text  = (current + 1) + " / " + pages.Count;
        if (navLabel != null) navLabel.text = "Página " + (current + 1) + " de " + pages.Count;

        pageContent.Clear();
        if (spec.right == null)
        {
            for (int k = 0; k < spec.left.Length; k++)
            {
                if (k > 0) pageContent.Add(Div("sec-divider"));
                pageContent.Add(RenderSectionBalanced(sections[byNum[spec.left[k]]]));
            }
        }
        else
        {
            var row = Div("book-columns");
            var cl  = Div("book-col");
            var cr  = Div("book-col");
            row.Add(cl); row.Add(Div("book-gutter")); row.Add(cr);
            FillColumn(cl, spec.left);
            FillColumn(cr, spec.right);
            pageContent.Add(row);
        }
        if (contentScroll != null) contentScroll.scrollOffset = Vector2.zero;
    }

    // Una columna del libro con una o varias secciones completas, apiladas.
    private void FillColumn(VisualElement col, string[] nums)
    {
        for (int i = 0; i < nums.Length; i++)
        {
            if (!byNum.TryGetValue(nums[i], out int si)) continue;
            if (i > 0) col.Add(Div("sec-divider"));
            col.Add(RenderSectionFlat(sections[si]));
        }
    }

    // ───────────────────────── render de sección ─────────────────────────
    private static readonly Regex CiteRx = new Regex(@"\s*\[\d+\]", RegexOptions.Compiled);

    // Quita los marcadores de cita [n] (son accesos directos a la bibliografía de la tesis,
    // en el software no son necesarios).
    private static string Strip(string s) => s == null ? null : CiteRx.Replace(s, "");

    private VisualElement SectionHeader(Section sec)
    {
        var box = new VisualElement();
        box.Add(Lbl(sec.num + "   " + sec.title, "sec-title"));
        box.Add(Div("separator-blue"));
        return box;
    }

    // Sección completa dentro de UNA columna (maqueta fija por lados).
    private VisualElement RenderSectionFlat(Section sec)
    {
        var box = SectionHeader(sec);
        foreach (var b in sec.blocks) box.Add(RenderBlock(b));
        return box;
    }

    private VisualElement RenderBlock(Block b)
    {
        switch (b.t)
        {
            case BType.Head:    return Lbl(Strip(b.text), "theory-h");
            case BType.Sub:     return Lbl(Strip(b.text), "theory-sub");
            case BType.Bullet:  return BuildBullet(Strip(b.text));
            case BType.Formula: return Lbl(Strip(b.text), "theory-formula");
            case BType.Figure:  return BuildFigButton(b);
            default:            return BuildJustified(Strip(b.text));
        }
    }

    // Átomo = unidad mínima para repartir en columnas. Los párrafos se trocean por oración
    // (así una sección de un solo párrafo también llena las DOS columnas = formato libro).
    private class Atom { public int src; public BType t; public string text; public Block block; }

    private static readonly Regex SentRx = new Regex(@"(?<=\.)\s+", RegexOptions.Compiled);

    // Sección que ocupa la página entera: se reparte por peso entre las dos columnas.
    private VisualElement RenderSectionBalanced(Section sec)
    {
        var box = SectionHeader(sec);

        var atoms = Atomize(sec.blocks);
        if (atoms.Count == 1)
        {
            box.Add(RenderAtom(atoms[0]));
        }
        else if (atoms.Count > 1)
        {
            var row = Div("book-columns");
            var cl  = Div("book-col");
            var cr  = Div("book-col");
            row.Add(cl); row.Add(Div("book-gutter")); row.Add(cr);

            float total = 0; foreach (var a in atoms) total += AtomWeight(a);
            float acc = 0; bool left = true;
            var li = new List<Atom>(); var ri = new List<Atom>();
            foreach (var a in atoms)
            {
                (left ? li : ri).Add(a);
                acc += AtomWeight(a);
                if (left && acc >= total * 0.5f) left = false;
            }
            RenderColumn(cl, li);
            RenderColumn(cr, ri);
            box.Add(row);
        }
        return box;
    }

    // Trocea bloques en átomos: párrafos por oración, figuras como un átomo-botón.
    private List<Atom> Atomize(List<Block> blocks)
    {
        var atoms = new List<Atom>();
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            if (b.t == BType.Para)
            {
                foreach (var s in SentRx.Split(Strip(b.text)))
                    if (!string.IsNullOrWhiteSpace(s))
                        atoms.Add(new Atom { src = i, t = BType.Para, text = s.Trim() });
            }
            else if (b.t == BType.Figure)
            {
                atoms.Add(new Atom { src = i, t = BType.Figure, block = b });
            }
            else
            {
                atoms.Add(new Atom { src = i, t = b.t, text = Strip(b.text) });
            }
        }
        return atoms;
    }

    // Renderiza una columna; fusiona oraciones contiguas del mismo párrafo en un bloque justificado.
    private void RenderColumn(VisualElement col, List<Atom> atoms)
    {
        int i = 0;
        while (i < atoms.Count)
        {
            var a = atoms[i];
            if (a.t == BType.Para)
            {
                var sb = new StringBuilder(a.text);
                int j = i + 1;
                while (j < atoms.Count && atoms[j].t == BType.Para && atoms[j].src == a.src)
                { sb.Append(' ').Append(atoms[j].text); j++; }
                col.Add(BuildJustified(sb.ToString()));
                i = j;
            }
            else { col.Add(RenderAtom(a)); i++; }
        }
    }

    private VisualElement RenderAtom(Atom a)
    {
        switch (a.t)
        {
            case BType.Head:    return Lbl(a.text, "theory-h");
            case BType.Sub:     return Lbl(a.text, "theory-sub");
            case BType.Bullet:  return BuildBullet(a.text);
            case BType.Formula: return Lbl(a.text, "theory-formula");
            case BType.Figure:  return BuildFigButton(a.block);
            default:            return BuildJustified(a.text);
        }
    }

    private static float AtomWeight(Atom a)
    {
        if (a.t == BType.Figure) return 150f;
        float w = (a.text != null ? a.text.Length : 0) + 40f;
        if (a.t == BType.Head || a.t == BType.Sub) w += 50f;
        return w;
    }

    // Botón que abre la figura (imagen/diagrama/tabla) en la ventana flotante.
    private VisualElement BuildFigButton(Block b)
    {
        var btn = new Button(() => OpenFig(b)) { text = "[ Ver ]   " + b.cap };
        btn.AddToClassList("fig-button");
        return btn;
    }

    // ───────────────────────── texto justificado ─────────────────────────
    // UI Toolkit no soporta -unity-text-align: justify, así que justificamos con flexbox:
    // cada palabra es un Label dentro de un contenedor flex-wrap con justify-content: space-between,
    // lo que reparte el sobrante entre las palabras de cada línea (borde derecho recto = justificado).
    // Un spacer flex-grow al final absorbe el hueco de la ÚLTIMA línea, dejándola alineada a la
    // izquierda (igual que en la tipografía justificada real, donde el último renglón no se estira).
    private VisualElement BuildJustified(string text)
    {
        var p = Div("theory-just");
        if (!string.IsNullOrEmpty(text))
            foreach (var w in text.Split(' '))
            {
                if (w.Length == 0) continue;
                var wl = new Label(w);
                wl.AddToClassList("theory-just-word");
                p.Add(wl);
            }
        p.Add(Div("theory-just-spacer"));
        return p;
    }

    private VisualElement BuildBullet(string text)
    {
        var row = Div("theory-bullet-row");
        row.Add(Lbl("•", "theory-bullet-mark"));
        var body = BuildJustified(text);
        body.style.flexGrow = 1;
        body.style.marginBottom = 0;
        row.Add(body);
        return row;
    }

    // ───────────────────────── ventana flotante de figura ─────────────────────────
    private Texture2D LoadFig(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!figCache.TryGetValue(key, out var tex))
        {
            tex = Resources.Load<Texture2D>(FigResourceFolder + key);
            if (tex == null) Debug.LogWarning("[TheoryReader] falta la imagen Resources/" + FigResourceFolder + key);
            figCache[key] = tex;
        }
        return tex;
    }

    // Contenido de la figura (la caption va en la barra de título del modal, no aquí).
    private VisualElement RenderFigureBody(Block b)
    {
        if (b.fkind == FKind.Image)
        {
            var img = Div("fig-image");
            var tex = LoadFig(b.imageKey);
            if (tex != null)
            {
                // Tamaño NATIVO de la imagen, sin reescalar (requisito del tutor).
                img.style.backgroundImage = new StyleBackground(tex);
                img.style.width = tex.width;
                img.style.height = tex.height;
            }
            else
            {
                img.style.width = 420f;
                img.style.height = 200f;
                var ph = Lbl("[ Imagen no asignada ]", "label-muted");
                ph.style.unityTextAlign = TextAnchor.MiddleCenter;
                img.Add(ph);
            }
            return img;
        }
        if (b.fkind == FKind.Diagram) return BuildDiagram(b);
        return BuildTable(b.table);
    }

    private void OpenFig(Block b)
    {
        if (figModal == null) return;
        if (figModalTitle != null) figModalTitle.text = b.cap;
        if (figModalContent != null)
        {
            figModalContent.Clear();
            figModalContent.Add(RenderFigureBody(b));
            figModalContent.scrollOffset = Vector2.zero;
        }
        figModal.AddToClassList("is-open");
    }

    private void CloseFig() { if (figModal != null) figModal.RemoveFromClassList("is-open"); }

    private static Label Lbl(string text, string cls)
    {
        var l = new Label(text);
        l.AddToClassList(cls);
        return l;
    }

    private static VisualElement Div(string cls)
    {
        var v = new VisualElement();
        v.AddToClassList(cls);
        return v;
    }

    private VisualElement BuildDiagram(Block b)
    {
        var w = Div("diag-wrap");
        foreach (var dr in b.diag)
        {
            if (!string.IsNullOrEmpty(dr.label))
                w.Add(Lbl(dr.label, "diag-rowlabel"));
            var row = Div("diag-row");
            if (dr.align == "left") row.AddToClassList("diag-row--left");
            else if (dr.align == "between") row.AddToClassList("diag-row--between");
            foreach (var it in dr.items) row.Add(BuildDItem(it));
            w.Add(row);
        }
        return w;
    }

    private VisualElement BuildDItem(DItem it)
    {
        switch (it.k)
        {
            case DItem.K.Box: return Lbl(it.text, "diag-box");
            case DItem.K.Acc:
            {
                var l = Lbl(it.text, "diag-box");
                l.AddToClassList("diag-box--accent");
                return l;
            }
            case DItem.K.Comp:
            {
                var c = Div("diag-compound");
                c.Add(Lbl(it.text, "diag-comp-title"));
                if (it.subs != null)
                    for (int s = 0; s < it.subs.Length; s++)
                    {
                        if (s > 0) c.Add(Lbl("↓", "diag-comp-arrow"));
                        c.Add(Lbl(it.subs[s], "diag-subbox"));
                    }
                return c;
            }
            case DItem.K.AR: return Lbl("→", "diag-arrow");
            case DItem.K.AL: return Lbl("←", "diag-arrow");
            case DItem.K.AD: return VArrow("↓", it.text);
            case DItem.K.AU: return VArrow("↑", it.text);
            case DItem.K.ABi: return VArrow("⟷", it.text);
            case DItem.K.Gap: return Div("diag-gap");
            default: return new VisualElement();
        }
    }

    private VisualElement VArrow(string glyph, string label)
    {
        if (string.IsNullOrEmpty(label)) return Lbl(glyph, "diag-arrow");
        var c = Div("diag-bi");
        c.Add(Lbl(glyph, "diag-arrow"));
        c.Add(Lbl(label, "diag-bi-label"));
        return c;
    }

    private VisualElement BuildTable(string[][] rows)
    {
        var t = Div("tbl");
        for (int r = 0; r < rows.Length; r++)
        {
            var row = Div("tbl-row");
            if (r == 0) row.AddToClassList("tbl-row--head");
            foreach (var cell in rows[r])
            {
                var c = Lbl(cell, "tbl-cell");
                if (r == 0) c.AddToClassList("tbl-cell--head");
                row.Add(c);
            }
            t.Add(row);
        }
        return t;
    }

    // ───────────────────────── índice ─────────────────────────
    private void OpenToc()
    {
        if (tocModal == null) return;
        if (!tocBuilt)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                int idx = i;
                var s = sections[i];
                bool sub = s.num.Split('.').Length > 2;
                var b = new Button(() => { CloseToc(); ShowPage(sectionToPage[idx]); }) { text = s.num + "      " + s.title };
                b.AddToClassList("toc-item");
                if (sub) b.AddToClassList("toc-item--sub");
                tocList.Add(b);
            }
            tocBuilt = true;
        }
        tocModal.AddToClassList("is-open");
    }

    private void CloseToc() { if (tocModal != null) tocModal.RemoveFromClassList("is-open"); }

    // ═════════════════════════ CONTENIDO COMPLETO (Marco Teórico, Cap. I) ═════════════════════════
    private List<Section> BuildContent()
    {
        System.Func<string, DItem> Bx = DItem.Box;
        System.Func<string, DItem> Ac = DItem.Acc;
        System.Func<DItem> AR = DItem.AR;
        System.Func<DItem> AL = DItem.AL;
        System.Func<string, DItem[], DRow> RowL = (lbl, it) => new DRow(lbl) { align = "left",    items = new List<DItem>(it) };
        System.Func<string, DItem[], DRow> RowB = (lbl, it) => new DRow(lbl) { align = "between", items = new List<DItem>(it) };

        var L = new List<Section>();

        L.Add(new Section { num = "1.1", title = "Satélite", blocks = new List<Block> {
            Block.P("Un satélite artificial es un objeto fabricado por el hombre y puesto en órbita para el estudio terrestre, espacial y el soporte de telecomunicaciones [1], campo en el que destacan antecedentes internacionales como el diseño de sistemas de radiación tipo Isoflux [2], la implementación de redes satelitales VSAT en la parroquia Columbe [3] y la simulación de enlaces de comunicación satelital [4]. A nivel nacional, la investigación se ha centrado en el desarrollo de plataformas virtuales de simulación y modelado 3D aplicadas al estudio de sistemas de comunicaciones móviles GSM [5], telefonía móvil, transporte y gestión [6], así como sistemas FTTX basados en tecnología GPON [7], herramientas que demuestran la vigencia y utilidad técnica de estos sistemas en el contexto actual."),
            Block.Img("Figura 1.1 — Partes de un satélite de comunicaciones", "fig_satelite_partes"),
        }});

        L.Add(new Section { num = "1.1.1", title = "Tipos de Satélites Según la Órbita", blocks = new List<Block> {
            Block.Bul("Satélites en Órbita Terrestre Baja (LEO): altitud aproximada de 160–1.500 km sobre la superficie de la Tierra. Tienen un periodo orbital corto, de entre 90 y 120 minutos, lo que significa que pueden dar la vuelta al planeta hasta 16 veces al día. Esto los hace especialmente adecuados para todo tipo de teledetección, observación terrestre de alta resolución e investigación científica, ya que los datos pueden obtenerse y transmitirse rápidamente [1]."),
            Block.Bul("Satélites en Órbita Terrestre Media (MEO): altitud de entre 5.000 y 20.000 km. Los servicios de posicionamiento y navegación, como el GPS, se apoyan habitualmente en satélites de tipo MEO. Gracias a su mayor período orbital (normalmente entre 2 y 12 horas), ofrece un término medio entre área de cobertura y velocidad de transmisión de datos. Comparados con los de órbita terrestre baja, los MEO necesitan menos aparatos para dar cobertura mundial, pero su retardo es mayor y su señal más débil [1]."),
            Block.Bul("Satélites en Órbita Geoestacionaria (GEO): la órbita geoestacionaria se sitúa a 35.786 km sobre la superficie terrestre, precisamente sobre el ecuador. Los objetos situados en esta órbita parecen inmóviles desde la Tierra porque su periodo orbital es idéntico a la rotación de la Tierra: 23 horas, 56 minutos y 4 segundos. Esto permite que una antena terrestre apunte siempre hacia el mismo dispositivo en el espacio. Por eso son perfectos para los servicios de comunicación siempre activos, como la televisión y los teléfonos. Además, pueden utilizarse en meteorología para monitorizar el tiempo en regiones concretas [1]."),
            Block.Bul("Satélites Heliosíncronos (SSO): atraviesan de norte a sur las regiones polares a una altitud de 600 a 800 km sobre la Tierra. La inclinación orbital y la altitud están calibradas de modo que siempre cruzan un lugar dado de forma precisa a la misma hora solar local. Así, las condiciones de iluminación son constantes para la obtención de imágenes, lo que lo hace ideal para la observación de la Tierra y la monitorización del medioambiente [1]."),
            Block.Img("Figura 1.2 — Tipos de satélites según la órbita", "fig_orbitas"),
        }});

        L.Add(new Section { num = "1.2", title = "Bandas de Frecuencia en Enlaces Satelitales", blocks = new List<Block> {
            Block.P("En Venezuela la distribución de frecuencias se rige por el Cuadro Nacional de Atribución de Bandas de Frecuencia (CUNABAF) [9], emitido por el ente regulador, la Comisión Nacional de Telecomunicaciones (CONATEL), el cual asigna los servicios permitidos en el territorio nacional. Para efectos de este trabajo se utilizará la nomenclatura estándar de bandas satelitales definida por la norma IEEE 521 [10], quedando establecidas las atribuciones legales venezolanas de la siguiente manera:"),
            Block.Tab("Tabla 1.1 — Clasificación de Bandas de Frecuencia", new string[][] {
                new [] {"Banda","Frecuencia Aprox.","Servicio CUNABAF","Uso Real en Venezuela"},
                new [] {"L","1.5 – 1.6 GHz","MSS (Móvil Satelital)","Telefonía satelital, GPS"},
                new [] {"C","3.4 – 6.4 GHz","FSS (Fijo Satelital)","TV cableras, enlaces de datos petroleros"},
                new [] {"Ku","10.7 – 14.5 GHz","BSS (Radiodifusión) / FSS","TV directa (SimpleTV), VSAT bancarios"},
                new [] {"Ka","17.7 – 30.0 GHz","FSS (Alta Densidad)","Internet satelital (ABA satelital, privados)"},
            }),
        }});

        L.Add(new Section { num = "1.3", title = "Sistemas Satelitales Operativos en Venezuela", blocks = new List<Block> {
            Block.P("Venezuela ha incursionado en la tecnología espacial mediante el uso de satélites diseñados para soberanía tecnológica en telecomunicaciones y observación. A continuación, se describen las características del único operativo actualmente:"),
            Block.P("El Satélite Antonio José de Sucre (VRSS-2) [11] es una plataforma de observación de la Tierra en órbita baja y heliosincrónica (LEO/SSO) que se encuentra actualmente operativa tras su lanzamiento el 9 de octubre de 2017, asumiendo formalmente las funciones del satélite Miranda (VRSS-1) luego de que este finalizara su ciclo de servicio. Este sistema optimiza la recolección de datos geoespaciales a través del uso de cámaras infrarrojas y de alta definición con capacidades de resolución mejoradas, empleando para su funcionamiento enlaces de datos especializados destinados tanto a la telemetría como a la transferencia efectiva de imágenes hacia las estaciones terrenas de control."),
        }});

        L.Add(new Section { num = "1.4", title = "Enlace Satelital", blocks = new List<Block> {
            Block.P("El enlace satelital es un canal por el cual se envían y/o reciben datos, los cuales viajan a través de señales de radiofrecuencia, que se emiten desde una estación terrena principal (telepuerto) hasta un satélite; este a su vez reenvía las señales a una estación remota ubicada en cualquier punto de la tierra. En la estación remota se convierten las señales en datos para tener acceso a Internet, telefonía, vídeo y streaming. Esto se logra por medio de un módem satelital [8]."),
        }});

        L.Add(new Section { num = "1.5", title = "Modelo del Enlace Satelital", blocks = new List<Block> {
            Block.P("Básicamente un enlace satelital se conforma de tres etapas. Dos están ubicadas en las estaciones terrestres, a las cuales llamaremos modelos de enlace de subida o bajada, y la tercera etapa estará ubicada en el espacio, donde la señal de subida cruzará por el transpondedor del satélite y será regresada a la tierra a una menor frecuencia con la que fue transmitida. En la siguiente imagen se muestra el modelo básico de un sistema satelital [12]."),
            Block.Img("Figura 1.3 — Modelo de enlace satelital", "fig_modelo_enlace"),
        }});

        L.Add(new Section { num = "1.5.1", title = "Estación Terrena", blocks = new List<Block> {
            Block.P("Los modelos tanto de subida como de bajada requieren de una estación terrena, ya sea para transmitir o para recibir una señal, y básicamente están compuestas de cuatro segmentos. El primer segmento es un modulador de FI para transmisión y, en el caso de recepción, un demodulador de FI. La segunda etapa es un convertidor elevador de FI a microondas RF para transmisión y, para la recepción, un convertidor descendente de RF a IF. La tercera es un amplificador de alta potencia (HPA) para transmisión y, para recepción, un amplificador de bajo ruido (LNA). Por último, la cuarta etapa son las antenas que conforman la estación terrena [12]."),
            Block.Diagram("Figura 1.4 — Estación Terrena",
                RowL("Transmisión (Tx)", new [] {
                    Bx("Señal de entrada"), AR(), Bx("Modulador de FI"), AR(), Bx("Convertidor elevador FI→RF"),
                    AR(), Bx("Amplificador de alta potencia (HPA)"), AR(), Bx("Antena transmisora"), AR(), Ac("Hacia el espacio (Subida)") }),
                RowL("Recepción (Rx)", new [] {
                    Bx("Señal de salida"), AL(), Bx("Demodulador de FI"), AL(), Bx("Convertidor descendente RF→IF"),
                    AL(), Bx("Amplificador de bajo ruido (LNA)"), AL(), Bx("Antena receptora"), AL(), Ac("Desde el espacio (Bajada)") })),
        }});

        L.Add(new Section { num = "1.5.2", title = "Modelo de Enlace de Subida", blocks = new List<Block> {
            Block.P("El enlace de subida consiste en modular una señal de FI en banda base a una señal de frecuencia intermedia modulada en FM, PSK, FSK ó QAM, seguida por el convertidor elevador, el cual está constituido por un mezclador y un filtro pasa bandas que convertirá la señal de IF a RF. Por último, la señal pasará por un amplificador de potencia (HPA), el cual le dará la potencia necesaria para que la señal llegue hasta el satélite [12]."),
            Block.Diagram("Figura 1.5 — Enlace de Subida",
                RowL(null, new [] {
                    Bx("Señal de banda base"), AR(), Bx("Modulador de FI (FM/PSK/FSK/QAM)"), AR(),
                    DItem.Comp("Convertidor Elevador", "Mezclador", "Filtro pasa bandas"), AR(),
                    Bx("Amplificador de potencia (HPA)"), AR(), Bx("Antena transmisora"), AR(), Ac("Hacia el satélite") })),
        }});

        L.Add(new Section { num = "1.5.3", title = "Transpondedor", blocks = new List<Block> {
            Block.P("El transpondedor está constituido por un filtro pasa bandas (BFP), el cual se encarga de limpiar el ruido que la señal adquiere en la trayectoria de subida, además de servir como seleccionador de canal, ya que cada canal satelital requiere un transpondedor por separado. Le sigue un amplificador de bajo ruido (LNA) y un desplazador de frecuencia, cuya función es convertir la frecuencia de banda alta de subida a banda baja de salida; después seguirá un amplificador de baja potencia que amplificará la señal de RF para el enlace de bajada, la señal será filtrada y regresada hacia la estación terrena [12]."),
            Block.Diagram("Figura 1.6 — Transpondedor",
                RowL(null, new [] {
                    Ac("Desde la Tierra"), AR(), Bx("Antena receptora satelital"), AR(), Bx("Filtro pasa bandas (BFP)"),
                    AR(), Bx("Amplificador de bajo ruido (LNA)"), AR(), Bx("Desplazador de frecuencia (banda alta → baja)"), DItem.AD() }),
                RowL(null, new [] {
                    Bx("Amplificador de baja potencia RF"), AR(), Bx("Filtro de salida"), AR(),
                    Bx("Antena transmisora satelital"), AR(), Ac("Hacia la Tierra") })),
        }});

        L.Add(new Section { num = "1.5.4", title = "Modelo de Enlace de Bajada", blocks = new List<Block> {
            Block.P("El receptor de la estación terrena contiene un filtro (BFP), el cual limita la potencia de entrada que recibe el LNA; una vez amplificada la señal en bajo ruido, será descendida de RF a frecuencias IF por medio de un convertidor descendente, después la señal será demodulada y entregada en banda base [12]."),
            Block.Diagram("Figura 1.7 — Enlace de Bajada",
                RowL(null, new [] {
                    Ac("Desde el satélite"), AR(), Bx("Antena receptora"), AR(), Bx("Filtro pasa bandas BFP (limita potencia)"),
                    AR(), Bx("Amplificador de bajo ruido (LNA)"), AR(), Bx("Convertidor descendente RF→IF"), AR(),
                    Bx("Demodulador"), AR(), Ac("Señal en banda base entregada") })),
        }});

        L.Add(new Section { num = "1.5.5", title = "Enlaces Cruzados", blocks = new List<Block> {
            Block.P("En ocasiones, para realizar una comunicación satelital no solo se va a requerir de un solo satélite; esto quiere decir que si no hay línea de vista entre el satélite y el receptor, se puede utilizar otro satélite que tenga línea de vista con la estación receptora, de este modo se podrán realizar transmisiones a mayores distancias [12]."),
            Block.Diagram("Figura 1.8 — Enlaces Cruzados",
                RowB(null, new [] {
                    Ac("Satélite 1 (sin línea de vista a ET Rx)"),
                    DItem.ABi("Enlace cruzado inter-satelital"),
                    Ac("Satélite 2 (con línea de vista a ET Rx)") }),
                RowB(null, new [] { DItem.AU("Enlace de subida"), DItem.AD("Enlace de bajada") }),
                RowB(null, new [] { Bx("Estación Terrena Transmisora (ET Tx)"), Bx("Estación Terrena Receptora (ET Rx)") })),
        }});

        L.Add(new Section { num = "1.6", title = "Transmisión de Datos", blocks = new List<Block> {
            Block.P("La transmisión de datos es el proceso de transferencia de datos digitales o analógicos entre dos o más dispositivos a través de un medio de comunicación, como cables de cobre, fibra óptica o señales inalámbricas. Implica la codificación de los datos en señales eléctricas, ópticas o de radio que se pueden transmitir a través de redes o canales. Durante este proceso, los datos originales se convierten en una señal adecuada para la transmisión, que luego se envía a través del medio de comunicación al dispositivo receptor. El dispositivo receptor decodifica la señal a su formato original o interpreta la información según el protocolo que se esté utilizando [13]."),
            Block.P("La transmisión de datos es esencial para los sistemas de comunicación modernos, ya que permite la transferencia de información entre dispositivos, redes y usuarios a lo largo de grandes distancias. Su importancia radica en la capacidad de facilitar el intercambio de datos en tiempo real, lo que respalda funciones críticas como la navegación por Internet, las videoconferencias, el intercambio de archivos y la nube informática [13]."),
            Block.Img("Figura 1.9 — Transmisión de datos: procesos y aplicaciones", "fig_transmision_datos"),
        }});

        L.Add(new Section { num = "1.6.1", title = "Etapas de la Transmisión de Datos", blocks = new List<Block> {
            Block.Bul("Modulación: permite enviar datos a través de ondas de radio u otros medios, convirtiendo la información a un formato adecuado para la transmisión. Es esencial para utilizar de forma eficiente el ancho de banda disponible y reducir las interferencias. Se compone de una señal de alta frecuencia, denominada portadora, la cual sufrirá la modificación de alguno de sus parámetros, siendo dicha modificación proporcional a la amplitud de la señal de baja frecuencia denominada moduladora. A la señal resultante se la denomina señal modulada y es la que se transmite [14]."),
            Block.Bul("Transmisión de la estación terrestre: una vez modulada la señal, se amplifica para garantizar que llegue con suficiente potencia al satélite, y se transmite a través de una antena diseñada en función de la frecuencia de trabajo de dicho enlace [13]."),
            Block.Bul("Recepción y retransmisión en el satélite: recibe las señales enviadas desde la estación terrestre y las remite a otro satélite o de vuelta a los receptores terrestres [13]."),
            Block.Bul("Recepción de la estación terrestre: la señal se recibe a través de las antenas, se demodula y se obtiene la señal original para conseguir la información enviada inicialmente [13]."),
        }});

        L.Add(new Section { num = "1.7", title = "Polarización de las Emisiones Satelitales", blocks = new List<Block> {
            Block.P("La polarización es la orientación del vector campo eléctrico de una onda electromagnética al propagarse. En los enlaces satelitales se utiliza para optimizar el uso del espectro radioeléctrico mediante la reutilización de frecuencias."),
            Block.Bul("Polarización Lineal (Horizontal y Vertical): comúnmente utilizada en la banda Ku."),
            Block.Bul("Polarización Circular (Derecha e Izquierda): preferida en bandas C y Ka, debido a que es menos susceptible a la rotación de Faraday producida por la ionosfera."),
            Block.P("El uso de polarizaciones ortogonales (transmitir simultáneamente en Horizontal y Vertical en la misma frecuencia) permite duplicar la capacidad de transporte de datos del sistema sin requerir ancho de banda adicional, siempre que exista un aislamiento suficiente entre polarizaciones para evitar la interferencia de polarización cruzada (XPI) [15]."),
            Block.Img("Figura 1.10 — Polarización y optimización del espectro", "fig_polarizacion"),
        }});

        L.Add(new Section { num = "1.8", title = "Figuras de Mérito y Desempeño de un Enlace Satelital", blocks = new List<Block> {
            Block.P("La calidad y eficiencia de un enlace satelital dependen de una serie de parámetros que intervienen en el proceso de comunicación:"),
            Block.S("Link Budget (Presupuesto de Enlace)"),
            Block.P("Es el cálculo detallado de todas las ganancias y pérdidas que experimenta una señal desde el transmisor hasta el receptor a través de un canal de comunicación. En ingeniería de telecomunicaciones es la herramienta fundamental para determinar si la potencia de la señal recibida es suficiente para superar el ruido y la interferencia, garantizando así una Tasa de Error de Bit (BER) aceptable para el transporte de datos [8]."),
            Block.For("Pr = Pt + Gt + Gr − Lp − La            (1.1)"),
            Block.Bul("Pr: Potencia recibida (dBW)."),
            Block.Bul("Pt: Potencia del transmisor (dBW)."),
            Block.Bul("Gt y Gr: Ganancias de las antenas de transmisión y recepción (dBi)."),
            Block.Bul("Lp: Pérdidas de trayectoria en el espacio libre (dB)."),
            Block.Bul("La: Pérdidas adicionales (atenuación atmosférica, absorción gaseosa e interferencias) (dB)."),
            Block.Bul("Relación Señal a Ruido (S/N o C/N): define la potencia de la portadora respecto a la densidad de ruido total (N = K · T · B). Es el indicador primario de la integridad del canal físico [16]."),
            Block.Bul("Figura de Mérito del Receptor (G/T): relación entre la ganancia de la antena y la temperatura de ruido del sistema. Es el parámetro que define la calidad de la estación terrena [16]."),
            Block.Bul("BER (Bit Error Rate): la métrica final de rendimiento digital. Se calcula en función de la modulación (Eb/N0); el software debe mostrar cómo un aumento en la interferencia degrada la tasa de error (ej. pasando de 10⁻⁹ a 10⁻³, lo que haría inútil el enlace) [16]."),
            Block.Img("Figura 1.11 — Link budget, pérdidas y desempeño digital", "fig_link_budget"),
        }});

        L.Add(new Section { num = "1.9", title = "Elementos Perjudiciales en un Enlace Satelital", blocks = new List<Block> {
            Block.Bul("Absorción Gaseosa: la señal sufre atenuación por la interacción con las moléculas de oxígeno (O₂) y vapor de agua (H₂O). Este efecto es no-lineal y depende de la frecuencia y el ángulo de elevación de la antena [17]."),
            Block.Bul("Ruido Blanco Gaussiano: en enlaces satelitales el ruido se modela principalmente como Ruido Blanco Gaussiano. Su impacto se mide a través de la temperatura de ruido del sistema (Tsys), que suma las contribuciones de la antena, el receptor (LNB) y el medio ambiente. Un incremento en el ruido térmico reduce la relación Portadora a Densidad de Ruido (C/N0); si el ruido supera el umbral de diseño, el receptor pierde el enganche de fase, provocando la pérdida total del flujo de datos [16]."),
            Block.Bul("Interferencia Co-canal (CCI): ocurre cuando dos señales usan la misma frecuencia y polarización. En el transporte de datos, esto causa una superposición en la constelación de modulación, aumentando drásticamente la Tasa de Error de Bit (BER) [16]."),
            Block.Bul("Interferencia de Polarización Cruzada (XPI): específicamente relevante cuando se usan polarizaciones ortogonales (como en los satélites Venesat-1 o VRSS-2). Si el aislamiento falla, la energía de una polarización se filtra en la otra, actuando como ruido adicional [16]."),
            Block.Img("Figura 1.12 — Elementos perjudiciales en un enlace satelital", "fig_elementos_perjudiciales"),
        }});

        L.Add(new Section { num = "1.10", title = "Métricas de Rendimiento y Calidad de Servicio (QoS)", blocks = new List<Block> {
            Block.Bul("Velocidad de Símbolos (Rs): se refiere a la cantidad de información que se puede transmitir por unidad de tiempo. Se mide en baudios (Bd) o símbolos por segundo, donde un símbolo representa o transmite uno o más bits de datos [18]."),
            Block.Bul("Velocidad de Transmisión (Rb): es la tasa de bits real. Depende de la modulación utilizada; por ejemplo, en QPSK cada símbolo transporta 2 bits [18]."),
            Block.Bul("Throughput (Tasa de Transferencia Efectiva): es la velocidad real de datos útiles tras descontar el encabezado de los protocolos y los bits de corrección de errores [18]."),
            Block.Bul("Latencia (Delay): es el tiempo total que transcurre desde que un bit o paquete de datos es enviado desde el origen hasta que es recibido correctamente en el destino [18]."),
            Block.Bul("Jitter: es la variación en el tiempo de llegada de los paquetes. Un jitter alto degrada la Calidad de Servicio (QoS), provocando cortes en voz sobre IP o video streaming [18]."),
            Block.Img("Figura 1.13 — Métricas de rendimiento y calidad de servicio", "fig_metricas_qos"),
        }});

        return L;
    }
}
