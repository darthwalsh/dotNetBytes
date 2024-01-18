import "./fileWatcher.js";

/** @typedef {object} CodeNode
 * @property {string} Name
 * @property {string} Description
 * @property {string} Value
 *
 * @property {number} Start absolute position of start
 * @property {number} End absolute position of end byte, exclusive
 * @property {?string} Ecma Section of ECMA-335 defining this node
 * @property {string[]} Errors
 *
 * @property {?string} LinkPath optional path like FileFormat/Array[2]/Flag
 * @property {string} [SelfPath] calculated
 * @property {string[]} [ReverseLinks] calculated
 * @property {CodeNode} [parent] calculated
 * @property {CodeNode[]} Children
 *
 * @property {HTMLLIElement} [tocLIdom] calculated
 */

/** @type {CodeNode} */
let FileFormat;

function assertThrow(message) {
  debugger;
  alert(message);
  throw message;
}

function $(id) {
  return document.getElementById(id);
}
/** @returns {HTMLElement} */
function create(tag, attr) {
  const el = document.createElement(tag);

  if (attr) {
    for (const key in attr) {
      el[key] = attr[key];
    }
  }

  return el;
}

// https://stackoverflow.com/a/8023734/771768
// 0 <= h, s, v <= 1
function HSVtoRGB(h, s, v) {
  let r, g, b, i, f, p, q, t;
  i = Math.floor(h * 6);
  f = h * 6 - i;
  p = v * (1 - s);
  q = v * (1 - f * s);
  t = v * (1 - (1 - f) * s);
  switch (i % 6) {
    case 0:
      r = v;
      g = t;
      b = p;
      break;
    case 1:
      r = q;
      g = v;
      b = p;
      break;
    case 2:
      r = p;
      g = v;
      b = t;
      break;
    case 3:
      r = p;
      g = q;
      b = v;
      break;
    case 4:
      r = t;
      g = p;
      b = v;
      break;
    case 5:
      r = v;
      g = p;
      b = q;
      break;
  }
  r = Math.round(r * 255);
  g = Math.round(g * 255);
  b = Math.round(b * 255);
  return "#" + r.toString(16) + g.toString(16) + b.toString(16);
}

function getColor(n) {
  return HSVtoRGB(getHue(n), 0.8, 1);
}

function getDimColor(n) {
  return HSVtoRGB(getHue(n), 0.3, 1);
}

/**
 * Picks colors that don't repeat.
 * [0, 1/3, 2/3, 1/6, 3/6, 5/6, 1/12, 3/12, 5/12, ...]
 * @param {number} n
 */
function getHue(n) {
  if (n < 3) return n / 3;
  let denominator = 6;
  n = 2 * n + 1 - 6;
  while (n >= denominator) {
    n -= denominator;
    denominator *= 2;
  }
  return n / denominator;
}

function byteID(i) {
  return "byte" + i;
}
function litID(i) {
  return "lit" + i;
}

//TODO(PERF) calling this function 100's of thousands of times results in
// a lot of unnecessary delay with style calculations. Instead, should only update if change is needed?
// Updating the color or the cursor causes the restyle cost. Updating half only costs half as much.
function setByte(i, color, cursor) {
  const byte = $(byteID(i));
  const lit = $(litID(i));

  byte.style.backgroundColor = color;
  lit.style.backgroundColor = color;

  byte.style.cursor = cursor;
  lit.style.cursor = cursor;
}

/** @param {CodeNode} node */
function scrollIntoView(node) {
  //TODO(PERF) could skipping getBoundingClientRect improve render time?
  const first = $(byteID(node.Start));
  const last = $(byteID(node.End - 1));

  const firstBox = first.getBoundingClientRect();
  const lastBox = last.getBoundingClientRect();

  // Need to scroll up
  if (lastBox.bottom < 0) {
    first.scrollIntoView(/* top: */ true);
  }

  // Need to scroll down
  if (firstBox.top > window.innerHeight) {
    last.scrollIntoView(/* top: */ false);
  }
}

/** @param {HTMLElement} el */
function hideToc(el) {
  el.style.visibility = "hidden";
  el.style.height = "0";
  el.style.lineHeight = "0";
}

/** @param {HTMLElement} el */
function showToc(el) {
  el.style.visibility = "";
  el.style.height = "";
  el.style.lineHeight = "";
}

function searchFilter() {
  const text = $("tocSearch").value.toLowerCase();

  allTocUL().forEach(hideToc);
  allTocLI().forEach(hideToc);

  // Unhide all the ToC up the parents
  const tocDiv = $("toc");
  for (const li of allTocLI()) {
    if (!li.textContent.toLowerCase().includes(text)) continue;

    let e = li;
    while (e !== tocDiv) {
      showToc(e);
      if (e.previousElementSibling) showToc(e.previousElementSibling);
      e = e.parentElement;
    }
    showToc(e);
  }
}

function linkFilter() {
  allTocUL().forEach(hideToc);
  allTocLI().forEach(hideToc);

  for (let i = FileFormat.Start; i < FileFormat.End; ++i) {
    setByte(i, "white", "auto");
  }

  linkFilterHelper(FileFormat, {n: 0});
}

/**
 * @param {CodeNode} node
 * @param {{n: number}} counter
 */
function linkFilterHelper(node, counter) {
  if (node.LinkPath) {
    for (let i = node.Start; i < node.End; ++i) {
      setByte(i, getColor(counter.n), "pointer");
    }
    ++counter.n;

    let e = node.tocLIdom;
    let first = true;
    while (e !== $("toc")) {
      showToc(e);
      if (e.previousElementSibling && !first) showToc(e.previousElementSibling);
      first = false;
      e = e.parentElement;
    }
  }

  for (const ch of node.Children) {
    linkFilterHelper(ch, counter);
  }
}

/** @param {CodeNode} node */
function setFocusObject(node) {
  let hash = "";
  for (let hashParent = node; hashParent; hashParent = hashParent.parent) {
    hash = hashParent.Name + "/" + hash;
  }
  window.location.hash = hash.substring(0, hash.length - 1);
}

/** @param {MouseEvent} ev */
function byteOnClick(ev) {
  /** @type {string} */
  const id = ev.target.id;

  const m = /(byte|lit)(\d+)/.exec(id);
  if (!m) return true;
  const n = Number(m[2]);

  if (isLinks()) {
    // Clicking on a link should focus the link source
    let node = FileFormat;
    while (node) {
      if (node.LinkPath) {
        window.location.hash = node.SelfPath;
        return true;
      }
      node = nodeChild(node, n);
    }
  } else {
    const node = currentNode();

    if (node.Start <= n && n < node.End && node.LinkPath) {
      window.location.hash = node.LinkPath;
      return true;
    }

    for (let parent = node; parent; parent = parent.parent) {
      const child = nodeChild(parent, n);
      if (!child) continue;

      if (child !== node) {
        setFocusObject(child);
      }
      return true;
    }
  }
}

/** @param {CodeNode} node */
function setFocus(node) {
  allTocUL().forEach(hideToc);
  // Unhide all the text elements
  allTocLI().forEach(showToc);

  // Unhide all the ToC up the parents
  const tocDiv = $("toc");
  const toc = node.tocLIdom;
  let parentUL = toc.parentElement;
  while (parentUL !== tocDiv) {
    showToc(parentUL);
    parentUL = parentUL.parentElement;
  }

  // Unhide sub-elements
  const sib = toc.nextElementSibling;
  if (sib) showToc(sib);

  allTocLI().forEach(li => {
    li.style.textDecoration = "";
    li.style.cursor = "";
  });
  toc.style.textDecoration = "underline";
  if (node.LinkPath) {
    toc.style.cursor = "pointer";
  }

  const byteSet = Array(FileFormat.End).fill(false);
  setFocusHelper(node, null, byteSet);

  for (let i = FileFormat.Start; i < FileFormat.End; ++i) {
    if (!byteSet[i]) {
      setByte(i, "white", "auto");
    }
  }

  scrollIntoView(node);

  drawDetails(node);
}

/** @param {CodeNode} node */
function drawDetails(node) {
  const focusDetail = $("focusDetail");
  while (focusDetail.firstChild) focusDetail.removeChild(focusDetail.firstChild);

  const detailDiv = createBasicDetailsDOM(node);
  focusDetail.appendChild(detailDiv);
  for (const line of node.Description.split("\n")) {
    const p = create("p", {textContent: line});
    p.style.margin = 0;
    detailDiv.appendChild(p);
  }

  if (node.ReverseLinks) {
    detailDiv.appendChild(create("p", {textContent: "Referenced by:"}));

    const ul = create("ul");
    for (const revLink of node.ReverseLinks) {
      const li = create("li");

      let prefix = 0;
      for (; prefix < revLink.length && prefix < node.SelfPath.length; ++prefix) {
        if (node.SelfPath[prefix] !== revLink[prefix]) break;
        // TODO this is buggy:
        /*
          if
            Method[1]/Header/CilOps/Op[0].Value 
          is a branch that links to
            Method[1]/Header/CilOps/Op[1]
          then the backref prefix will be
            0].Value
        */
      }
      li.appendChild(
        create("a", {
          href: "#" + revLink,
          textContent: revLink.substring(prefix),
        })
      );

      ul.appendChild(li);
    }
    detailDiv.appendChild(ul);
  }
}
function isLinks() {
  return window.location.href.split("#")[1] === "/Links";
}
function onHashChange() {
  if (isLinks()) {
    linkFilter();
  } else {
    setFocus(currentNode());
  }
}

function currentNode() {
  if (isLinks()) {
    return FileFormat;
  }

  const hash = window.location.href.split("#")[1];
  return hash ? lookupNode(hash) : FileFormat;
}

/** @param {string} path */
function lookupNode(path) {
  const names = path.split("/");

  let o = FileFormat;
  if (names[0] != "FileFormat") {
    // MAYBE when reloading with a new binary, select the first parent node?
    assertThrow(`Couldn't find ${names[0]} under ${o.Name}`);
  }
  names.slice(1).forEach(name => {
    const [c] = o.Children.filter(c => name == c.Name);
    if (!c) {
      assertThrow(`Couldn't find ${name} under ${o.Name}`);
    }
    o = c;
  });
  return o;
}

/**
 * @param {CodeNode} node
 * @param {Number} n
 * @returns {?CodeNode}
 */
function nodeChild(node, n) {
  for (const child of node.Children) {
    if (child.Start <= n && n < child.End) {
      return child;
    }
  }
}

/**
 * @param {CodeNode} node
 * @param {?CodeNode} currentChild
 * @param {boolean[]} byteSet
 */
function setFocusHelper(node, currentChild, byteSet) {
  if (node.parent) {
    setFocusHelper(node.parent, node, byteSet);
  }

  const ch = node.Children;

  let col;
  for (let chI = 0; chI < ch.length; ++chI) {
    const cc = ch[chI];
    if (cc === currentChild) {
      continue;
    }

    col = currentChild ? getDimColor(chI) : getColor(chI);

    for (let i = cc.Start; i < cc.End; ++i) {
      setByte(i, col, "zoom-in");
      byteSet[i] = true;
    }
  }

  if (!currentChild && !ch.length) {
    col = getColor(0); //TODO in-order coloring
    const cursor = node.LinkPath ? "pointer" : "auto";
    for (let i = node.Start; i < node.End; ++i) {
      setByte(i, col, cursor);
      byteSet[i] = true;
    }
  }
}

/** @param {CodeNode} node */
function addParent(node) {
  for (const c of node.Children) {
    c.parent = node;
    addParent(c);
  }
}

/** @param {CodeNode} node */
function setSelfPaths(node, prefix) {
  if (prefix) prefix += "/";
  prefix = prefix || "";
  prefix += node.Name;

  node.SelfPath = prefix;

  for (const c of node.Children) {
    setSelfPaths(c, prefix);
  }
}

/** @param {CodeNode} node */
function findLinkReferences(node) {
  if (node.LinkPath) {
    const linked = lookupNode(node.LinkPath);

    linked.ReverseLinks = linked.ReverseLinks || [];
    linked.ReverseLinks.push(node.SelfPath);
  }

  node.Children.forEach(findLinkReferences);
}

function drawToc() {
  const ul = create("ul"); // MAYBE would nested <details> be simpler? https://developer.mozilla.org/en-US/docs/Web/HTML/Element/details
  $("toc").appendChild(ul);

  drawTocHelper(FileFormat, ul);
}

/** @param {CodeNode} node */
function drawTocHelper(node, parentUL) {
  const onclick = _ => {
    if (node == currentNode() && node.LinkPath) {
      window.location.hash = node.LinkPath;
    } else {
      setFocusObject(node);
    }
  };
  const li = create("li", {textContent: node.Name, onclick});
  if (node.LinkPath) {
    li.style.color = "blue";
  }

  node.tocLIdom = li;

  parentUL.appendChild(li);

  const ch = node.Children;

  if (ch.length) {
    const ul = create("ul");
    for (const c of ch) {
      drawTocHelper(c, ul);
    }
    parentUL.appendChild(ul);
  }
}

/** @param {CodeNode} node */
function createBasicDetailsDOM(node) {
  const details = create("div", {onclick: _ => setFocusObject(node)});
  const name = create("p", {textContent: node.Name});
  if (node.Ecma) {
    name.textContent += " ";
    const href = `https://darthwalsh.github.io/ecma-335?section=${node.Ecma}`;
    const ecmaLink = create("a", {href, target: "_blank"});

    const ecma = create("img", {src: "ecma.png"});
    ecma.style.height = "1em";
    ecmaLink.appendChild(ecma);
    ecmaLink.append(`§${node.Ecma}`);
    name.appendChild(ecmaLink);
  }
  details.appendChild(name);
  details.appendChild(create("p", {textContent: node.Value}));
  return details;
}

/** @param {CodeNode} node */
function findErrors(node) {
  for (const err of node.Errors) {
    const errorDiv = createBasicDetailsDOM(node);
    $("details").appendChild(errorDiv);
    errorDiv.classList.add("error");

    errorDiv.appendChild(create("p", {textContent: err}));
  }

  node.Children.forEach(findErrors);
}

function ToHex(code, width) {
  const hexEncodeArray = "0123456789ABCDEF".split("");

  let s = "";
  for (let i = 0; i < width || code; ++i) {
    s = hexEncodeArray[code & 0x0f] + s;
    code >>>= 4;
  }

  return s;
}

// Like ASCII, but the upper range has glyphs included in the font.
const charEncoding =
  ".αβγδεζηθικλμξπφ" +
  "χψωΓΔΞΠΣΦΨΩ♠♥♦♣∞" +
  " !\"#$%&'()*+,-./" +
  "0123456789:;<=>?" +
  "@ABCDEFGHIJKLMNO" +
  "PQRSTUVWXYZ[\\]^_" +
  "`abcdefghijklmno" +
  "pqrstuvwxyz{|}~₪" +
  "◦ƒ¨“”♂‡–ˆ‰Š‹Œ♫Ž¬" +
  "฿₱₩‘’♀†₸∫™š›œ♪žß" +
  "€¡¢£¤¥¦§◊©ª«¯₹·¸" +
  "±°¹²³´…¶≠®º»☼¼¾¿" +
  "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏ" +
  "àáâãäåæçèéêëìíîï" +
  "ÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞŸ" +
  "ðñòóôõö÷øùúûüýþÿ";

function removeChildren(id) {
  const el = $(id);
  while (el.firstChild) el.removeChild(el.firstChild);
}

function removeErrorDetails() {
  const details = $("details");
  while (details.firstElementChild.nextElementSibling)
    details.removeChild(details.firstElementChild.nextElementSibling);
}

function allTocUL() {
  return $("toc").querySelectorAll("ul");
}

function allTocLI() {
  return $("toc").querySelectorAll("li");
}

function cleanupDisplay() {
  removeChildren("tocSearch");
  removeChildren("toc");
  removeChildren("bytes");
  removeChildren("focusDetail");
  removeErrorDetails();
}

/** @param {ArrayBuffer} buf */
function displayHex(buf) {
  const bytes = new Uint8Array(buf);
  const div = $("bytes");
  const width = 16;
  const size = bytes.length;

  const rowLabelWidth = ToHex(size - 1).length;

  div.appendChild(create("code", {textContent: Array(rowLabelWidth + 1).join("-") + "    "}));

  for (let i = 0; i < width; i++) {
    div.appendChild(create("code", {textContent: ToHex(i, 2) + " "}));
  }

  div.appendChild(create("br"));

  for (let j = 0; j < size; j += width) {
    div.appendChild(create("code", {textContent: ToHex(j, rowLabelWidth) + "    "}));

    for (let i = j; i - j < width; i++) {
      div.appendChild(
        create("code", {
          textContent: ToHex(bytes[i], 2) + " ",
          id: byteID(i),
        })
      );
    }

    div.appendChild(create("code", {innerHTML: "&nbsp;&nbsp;&nbsp;&nbsp;"}));

    for (let i = j; i - j < width; i++) {
      const lit = charEncoding[bytes[i]];
      div.appendChild(create("code", {textContent: lit, id: litID(i)}));
    }

    div.appendChild(create("br"));
  }

  div.onclick = byteOnClick;
}

/** @param {CodeNode} o */
function displayParse(o) {
  FileFormat = o;

  addParent(FileFormat);
  setSelfPaths(FileFormat);
  findLinkReferences(FileFormat);
  drawToc();
  findErrors(FileFormat);

  window.onhashchange = onHashChange;

  $("tocSearch").oninput = searchFilter;
  $("visualLinks").onclick = _ => (window.location.hash = "/Links");

  window.onhashchange();
}

async function setupExampleBytes() {
  const bytesResponse = await fetch("Program.dat");
  const json = await fetch("bytes.json");

  const buf = await bytesResponse.arrayBuffer();
  displayHex(buf);
  displayParse(await json.json());
}

async function setupFromFile(buf) {
  displayHex(buf);

  const parseUrl = window.location.href.startsWith("http://localhost:5500")
    ? "http://127.0.0.1:8080"
    : "https://us-central1-dotnetbytes.cloudfunctions.net/parse";

  const response = await fetch(parseUrl, {
    method: "POST",
    body: buf,
    headers: {
      "Content-Type": "application/x-msdownload",
    },
  });
  displayParse(await response.json());
}

const exampleButton = $("example");
const fileInput = $("fileInput");

/*
  If URL query is blank:
  - Wait for the user to input a file
  - Post file to get JSON form parse 
  If URL has ?Example
  - Get EXE from Program.dat
  - Get JSON parse from bytes.json
*/

if (!window.location.href.includes("?Example=true")) {
  exampleButton.onclick = () => {
    let hash = window.location.href.split("#")[1] || "";
    if (hash) {
      hash = "#" + hash; //TODO this often causes an error -- go to top?
    }
    window.location.href = "?Example=true" + hash;
  };
  exampleButton.value = "Try Example";

  fileInput.addEventListener("change", async ev => {
    /** @type {ArrayBuffer} */
    const buf = await ev.bytes.arrayBuffer();
    if (!buf.byteLength) return;

    cleanupDisplay();

    const dateString = ev.bytes.lastModifiedDate.toLocaleString([], {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
    //TODO(API) pseudoelement on the file changing componenet?
    $("fileTime").innerText = ev.bytes.name + " " + dateString;
    fileInput.style.display = "none";

    setupFromFile(buf);
  });
} else {
  exampleButton.onclick = () => {
    window.location.href = window.location.href.replace("?Example=true", "");
  };
  exampleButton.value = "Leave Example";
  fileInput.style.display = "none";

  setupExampleBytes();
}

//TODO rightPanel width isn't always wide enough for link text i.e. Methods/Method[0]/CilOps/Op[2]/Token/Offset
//TODO(LINK) link targets, using dim? What if both?
//TODO(LINK) visualize link sizes (using onhover to highlight size of target)
//TODO link-references should include name of linking object instead of path
//TODO smart colors (better saturations on reds, etc.) (maybe 0, 120, 240, 60, 180, 300, 30, 90, etc?)
//TODO onhover tooltip to update details div on right
//TODO method ops and stack state visualization before op and links between branches, e.g.
//  load 10 |
//  load 2.0| I4
//  add     | I4 R8
//  ret     | R8
//TODO split monolithic JS file into little libraries

//MAYBE search boxes: X to close, should filter colored bytes
//MAYBE search also looks through name/details (optional check box?) (heuristic to show below?)
