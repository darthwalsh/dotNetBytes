"use strict";

/** @typedef {object} CodeNode
 * @property {string} Name
 * @property {string} Description
 * @property {string} Value
 *
 * @property {number} Start absolute position of start
 * @property {number} End absolute position of end byte, exclusive
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

const profile = false;
function time(name) {
  if (profile) {
    console.time(name);
  }
}

function timeEnd(name) {
  if (profile) {
    console.timeEnd(name);
  }
}

function $(id) {
  return document.getElementById(id);
}
function create(tag, attr) {
  const el = document.createElement(tag);

  if (attr) {
    for (const key in attr) {
      el[key] = attr[key];
    }
  }

  return el;
}

// http://stackoverflow.com/a/8023734/771768
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

//TODO(PERF) calling this function 10's of thousands of times results in
// a lot of unnecessary delay with style calculations. Instead, should only update if change is needed?
// Updating the color or the cursor causes the restyle cost. Updating half only costs half as much.
// Could probably just avoid setting if no change is needed (remove the other code that sets to white color?)
function setByte(i, color, onclick, cursor) {
  const byte = $(byteID(i));
  const lit = $(litID(i));

  byte.style.backgroundColor = color;
  lit.style.backgroundColor = color;

  byte.onclick = onclick;
  lit.onclick = onclick;

  byte.style.cursor = cursor;
  lit.style.cursor = cursor;
}

/** @param {CodeNode} node */
function scrollIntoView(node) {
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

function searchFilter() {
  const text = $("tocSearch").value.toLowerCase();

  allTocUL().forEach(ul => (ul.style.display = "none"));
  allTocLI().forEach(li => (li.style.display = "none"));

  // Unhide all the ToC up the parents
  const tocDiv = $("toc");
  for (const li of allTocLI()) {
    if (!li.textContent.toLowerCase().includes(text)) continue;

    let e = li;
    while (e !== tocDiv) {
      e.style.display = "";
      if (e.previousElementSibling) e.previousElementSibling.style.display = "";
      e = e.parentElement;
    }
    e.style.display = "";
  }
}

function linkFilter() {
  allTocUL().forEach(ul => (ul.style.display = "none"));
  allTocLI().forEach(li => (li.style.display = "none"));

  for (let i = FileFormat.Start; i < FileFormat.End; ++i) {
    setByte(i, "white", null, "auto");
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
      setByte(i, getColor(counter.n), _ => (window.location.hash = node.SelfPath), "pointer");
    }
    ++counter.n;

    let e = node.tocLIdom;
    while (e !== $("toc")) {
      e.style.display = "";
      if (e.previousElementSibling) e.previousElementSibling.style.display = "";
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

/** @param {CodeNode} node */
function setFocus(node) {
  allTocUL().forEach(ul => (ul.style.display = "none"));
  // Unhide all the text elements
  allTocLI().forEach(li => (li.style.display = ""));

  // Unhide all the ToC up the parents
  const tocDiv = $("toc");
  const toc = node.tocLIdom;
  let parentUL = toc.parentElement;
  while (parentUL !== tocDiv) {
    parentUL.style.display = "";
    parentUL = parentUL.parentElement;
  }

  // Unhide sub-elements
  const sib = toc.nextElementSibling;
  if (sib) {
    sib.style.display = "";
  }

  allTocLI().forEach(li => (li.style.textDecoration = ""));
  toc.style.textDecoration = "underline";

  // Reset all the byte display
  for (let i = FileFormat.Start; i < FileFormat.End; ++i) {
    setByte(i, "white", null, "auto");
  }

  time("setFocusHelper");
  setFocusHelper(node);
  timeEnd("setFocusHelper");

  time("scrollIntoView");
  scrollIntoView(node);
  timeEnd("scrollIntoView");

  drawDetails(node);
}

/** @param {CodeNode} node */
function drawDetails(node) {
  const focusDetail = $("focusDetail");
  while (focusDetail.firstChild) focusDetail.removeChild(focusDetail.firstChild);

  const detailDiv = createBasicDetailsDOM(node);
  focusDetail.appendChild(detailDiv);
  detailDiv.appendChild(create("p", {textContent: node.Description}));

  if (node.ReverseLinks) {
    detailDiv.appendChild(create("p", {textContent: "Referenced by:"}));

    const ul = create("ul");
    for (const revLink of node.ReverseLinks) {
      const li = create("li");

      let prefix = 0;
      for (; prefix < revLink.length && prefix < node.SelfPath.length; ++prefix) {
        if (node.SelfPath[prefix] !== revLink[prefix]) break;
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

/** @param {CodeNode} node */
function makeOnClick(node) {
  return _ => setFocusObject(node);
}

function onHashChange() {
  time("makeOnHashChange");
  const hash = window.location.href.split("#")[1] || "FileFormat";

  if (hash === "/Links") {
    linkFilter();
  } else {
    setFocus(lookupNode(hash));
  }

  timeEnd("makeOnHashChange");
}

/** @param {string} path */
function lookupNode(path) {
  const names = path.split("/");

  let o = FileFormat;
  if (names[0] != "FileFormat") {
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
 * @param {?CodeNode} currentChild
 */
function setFocusHelper(node, currentChild) {
  if (node.parent) {
    setFocusHelper(node.parent, node);
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
      setByte(i, col, makeOnClick(cc), "zoom-in");
    }
  }

  if (!currentChild && !ch.length) {
    col = getColor(0); //TODO in-order coloring
    let onclick = null;
    let cursor = "auto";
    if (node.LinkPath) {
      onclick = _ => (window.location.hash = node.LinkPath);
      cursor = "pointer";
    }
    for (let i = node.Start; i < node.End; ++i) {
      setByte(i, col, onclick, cursor);
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
  const ul = create("ul");
  $("toc").appendChild(ul);

  drawTocHelper(FileFormat, ul);

  $("bytes").style.marginLeft = $("toc").scrollWidth + 20 + "px";
}

/** @param {CodeNode} node */
function drawTocHelper(node, parentUL) {
  const li = create("li", {textContent: node.Name, onclick: makeOnClick(node)});

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
  const details = create("div", {onclick: makeOnClick(node)});
  details.appendChild(create("p", {textContent: node.Name}));
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

// Like ASCII, but with other nice-width glyphs instead of unprintable characters
const font =
  ".αβγδεζηθικλμξπφ" +
  "χψωΓΔΞΠΣΦΨΩ♠♥♦♣∞" +
  " !\"#$%&'()*+,-./" +
  "0123456789:;<=>?" +
  "@ABCDEFGHIJKLMNO" +
  "PQRSTUVWXYZ[\\]^_" +
  "`abcdefghijklmno" +
  "pqrstuvwxyz{|}~₪" +
  "◦ƒ‽“”♂‡–ˆ‰Š‹Œ♫Ž¬" +
  "฿₱₩‘’♀†₸∫™š›œ♪žß" +
  "€¡¢£¤¥¦§◊©ª«✶₹⸗Ⅎ" +
  "±°¹²³´…¶≠®º»☼ⱷꜘ¿" +
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

function displayHex(bytes) {
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
      const lit = font[bytes[i]];
      div.appendChild(create("code", {textContent: lit, id: litID(i)}));
    }

    div.appendChild(create("br"));
  }
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

// listenFileChange and fileChange
// Copyright Microsoft, 2017
// Copyright Carl Walsh, 2021
/**
 * @param {HTMLInputElement} el
 * @param {number} pollMS
 * @param {function} bytesCallBack
 */
function listenFileChange(el, pollMS, bytesCallBack) {
  const time = document.createElement("span");
  time.innerText = "...";
  el.parentNode.insertBefore(time, el.nextSibling);

  el.addEventListener("change", evt =>
    fileChange(evt, pollMS, file => {
      if (!file) {
        return;
      }
      time.innerText = file.lastModifiedDate.toLocaleString([], {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
      });

      const fileReader = new FileReader();
      fileReader.onload = () => bytesCallBack(new Uint8Array(this.result));
      fileReader.readAsArrayBuffer(file);
    })
  );
}

function fileChange(evt, pollMS, fileCallback) {
  /** @type {FileList} */
  const files = evt.target.files;
  const f = files[0];

  fileCallback(f);
  /** @type {Date} */
  const lastModified = f.lastModifiedDate;

  setInterval(() => {
    if (f.lastModifiedDate.getTime() !== lastModified.getTime()) {
      lastModified = f.lastModifiedDate;
      fileCallback(f);
    }
  }, pollMS);
}

function readExampleBytes(callback) {
  const file = "Program.dat";

  const req = new XMLHttpRequest();
  req.open("GET", file);
  req.responseType = "arraybuffer";
  req.onload = () => {
    if (req.status === 200 && req.response) {
      callback(new Uint8Array(req.response));
    } else {
      assertThrow("Couldn't find " + file);
    }
  };
  req.send();
}

function readExampleJson(callback) {
  const file = "bytes.json";

  const req = new XMLHttpRequest();
  req.open("GET", file);
  req.onload = () => {
    if (req.status === 200 && req.responseText) {
      callback(JSON.parse(req.responseText));
    } else {
      assertThrow("Couldn't find " + file);
    }
  };
  req.send();
}

function parseFile(bytes, callback) {
  const req = new XMLHttpRequest();
  req.open("POST", "parse", true);
  req.setRequestHeader("Content-type", "application/x-msdownload");
  req.onload = () => {
    if (this.status === 200 && req.responseText) {
      callback(JSON.parse(req.responseText));
    } else {
      assertThrow("Error from web server: " + this.status + "\n" + req.responseText);
    }
  };
  req.send(bytes);
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
      hash = "#" + hash;
    }
    window.location.href = "?Example=true" + hash;
  };
  exampleButton.value = "Try Example";

  listenFileChange(fileInput, 2000, bytes => {
    cleanupDisplay();
    displayHex(bytes);
    parseFile(bytes, displayParse);
  });
} else {
  exampleButton.onclick = () => {
    window.location.href = window.location.href.replace("?Example=true", "");
  };
  exampleButton.value = "Leave Example";
  fileInput.style.display = "none";

  readExampleBytes(bytes => {
    cleanupDisplay();
    displayHex(bytes);
    readExampleJson(displayParse);
  });
}

//TODO(HACK) method signatures
//TODO(ACCS) ? avoid red/gree color palette
//TODO(HACK) layout bytes dynamically (laptop / smartphone screen) 8 / 16 / 32 bytes wide
//TODO(ACCS) keyboarding through ToC
//TODO(LINK) link targets, using dim? What if both?
//TODO(LINK) visualize link sizes
//TODO link-references should include name of linking object instead of path
//TODO smart colors (better saturations on reds, etc.) (maybe 0, 120, 240, 60, 180, 300, 30, 90, etc?)
//TODO hover preview
//TODO(BUG) large lists cause ugly scroll menu?
//TODO resize ToC dynamically (PERF can load ToC lazily?)
//TODO PERF on click only update cells that need to change?
//TODO PERF use one big click handler instead of thousands of little ones
//TODO magnifying glass in search boxes, X to close, should filter colored bytes, should set URL
//TODO search also looks through name/details (optional check box?) (heuristic to show below?)
//TODO method ops and stack state visualization before op and links between branches, e.g.
//  load 10 |
//  load 2.0| I4
//  add     | I4 R8
//  ret     | R8
//TODO favorites "shortcut" pinning
//TODO details pinning + clearing
//TODO split monolithic JS file into little libraries
//  TODO try out file watcher library upload experience in FireFox
//TODO visualize version number
