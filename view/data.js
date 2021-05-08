"use strict";

var global;

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
  var el = document.createElement(tag);

  if (attr) {
    for (var key in attr) {
      el[key] = attr[key];
    }
  }

  return el;
}

// http://stackoverflow.com/a/8023734/771768
// 0 <= h, s, v <= 1
function HSVtoRGB(h, s, v) {
  var r, g, b, i, f, p, q, t;
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
  return HSVtoRGB(n / 12, 0.8, 1);
}

function getDimColor(n) {
  return HSVtoRGB(n / 12, 0.3, 1);
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
  var byte = $(byteID(i));
  var lit = $(litID(i));

  byte.style.backgroundColor = color;
  lit.style.backgroundColor = color;

  byte.onclick = onclick;
  lit.onclick = onclick;

  byte.style.cursor = cursor;
  lit.style.cursor = cursor;
}

function scrollIntoView(o) {
  var first = $(byteID(o.Start));
  var last = $(byteID(o.End - 1));

  var firstBox = first.getBoundingClientRect();
  var lastBox = last.getBoundingClientRect();

  // Need to scroll up
  if (lastBox.bottom < 0) {
    first.scrollIntoView(/* top: */ true);
  }

  // Need to scroll down
  if (firstBox.top > window.innerHeight) {
    last.scrollIntoView(/* top: */ false);
  }
}

function Search() {
  var text = $("tocSearch").value.toLowerCase();

  // Hide all the ToC
  for (var i = 0; i < global.allTocUL.length; ++i) {
    global.allTocUL[i].style.display = "none";
  }
  for (i = 0; i < global.allTocLI.length; ++i) {
    global.allTocLI[i].style.display = "none";
  }

  // Unhide all the ToC up the parents
  var tocDiv = $("toc");
  for (i = 0; i < global.allTocLI.length; ++i) {
    var li = global.allTocLI[i];
    if (li.textContent.toLowerCase().indexOf(text) === -1) continue;

    while (li !== tocDiv) {
      li.style.display = "";
      if (li.previousElementSibling) li.previousElementSibling.style.display = "";
      li = li.parentElement;
    }
    li.style.display = "";
  }
}

function setFocusObject(o) {
  var hash = "";
  for (var hashParent = o; hashParent; hashParent = hashParent.parent) {
    hash = hashParent.Name + "/" + hash;
  }
  window.location.hash = hash.substring(0, hash.length - 1);
}

function setFocus(o) {
  // Hide all lists in the ToC
  for (var i = 0; i < global.allTocUL.length; ++i) {
    global.allTocUL[i].style.display = "none";
  }
  // Unhide all the text elements
  for (i = 0; i < global.allTocLI.length; ++i) {
    global.allTocLI[i].style.display = "";
  }

  // Unhide all the ToC up the parents
  var tocDiv = $("toc");
  var toc = o.tocDom;
  var parentUL = toc.parentElement;
  while (parentUL !== tocDiv) {
    parentUL.style.display = "";
    parentUL = parentUL.parentElement;
  }

  // Unhide sub-elements
  var sib = toc.nextElementSibling;
  if (sib) {
    sib.style.display = "";
  }

  // Underline the current li
  for (i = 0; i < global.allTocLI.length; ++i) {
    global.allTocLI[i].style.textDecoration = "";
  }
  toc.style.textDecoration = "underline";

  // Reset all the byte display
  var grandparent = o;
  while (grandparent.parent) grandparent = grandparent.parent;
  for (i = grandparent.Start; i < grandparent.End; ++i) {
    setByte(i, "white", null, "auto");
  }

  time("setFocusHelper");
  setFocusHelper(o);
  timeEnd("setFocusHelper");

  time("scrollIntoView");
  scrollIntoView(o);
  timeEnd("scrollIntoView");

  drawDetails(o);
}

function drawDetails(o) {
  var focusDetail = $("focusDetail");
  while (focusDetail.firstChild) focusDetail.removeChild(focusDetail.firstChild);

  var detailDiv = createBasicDetailsDOM(focusDetail, o);
  detailDiv.appendChild(create("p", {textContent: o.Description}));

  if (o.ReverseLinks) {
    detailDiv.appendChild(create("p", {textContent: "Referenced by:"}));

    var ul = create("ul");
    for (var i = 0; i < o.ReverseLinks.length; ++i) {
      var li = create("li");

      var prefix = 0;
      for (; prefix < o.ReverseLinks[i].length && prefix < o.NodePath.length; ++prefix) {
        if (o.NodePath[prefix] !== o.ReverseLinks[i][prefix]) break;
      }
      li.appendChild(
        create("a", {
          href: "#" + o.ReverseLinks[i],
          textContent: o.ReverseLinks[i].substring(prefix),
        })
      );

      ul.appendChild(li);
    }
    detailDiv.appendChild(ul);
  }
}

function makeOnClick(o) {
  return _ => setFocusObject(o);
}

function makeOnHashChange(json) {
  return _ => {
    time("makeOnHashChange");
    var hash = window.location.href.split("#")[1];
    var names = hash.split("/");

    var o = json;
    for (var i = 1; i < names.length; ++i) {
      for (var chi = 0; ; ++chi) {
        if (chi === o.Children.length) {
          assertThrow("Couldn't find " + names[i] + " under " + o.Name);
          return;
        }

        if (names[i] === o.Children[chi].Name) {
          o = o.Children[chi];
          break;
        }
      }
    }

    setFocus(o);

    timeEnd("makeOnHashChange");
  };
}

function setFocusHelper(o, currentChild) {
  if (o.parent) {
    setFocusHelper(o.parent, o);
  }

  var ch = o.Children;

  for (var chI = 0; chI < ch.length; ++chI) {
    var cc = ch[chI];
    if (cc === currentChild) {
      continue;
    }

    var col = currentChild ? getDimColor(chI) : getColor(chI);

    for (var i = cc.Start; i < cc.End; ++i) {
      setByte(i, col, makeOnClick(cc), "zoom-in");
    }
  }

  if (!currentChild && !ch.length) {
    col = getColor(0); //TODO in-order coloring
    var onclick = null;
    var cursor = "auto";
    if (o.LinkPath) {
      onclick = _ => window.location.hash = o.LinkPath;
      cursor = "pointer";
    }
    for (i = o.Start; i < o.End; ++i) {
      setByte(i, col, onclick, cursor);
    }
  }
}

function addParent(json) {
  for (var i = 0; i < json.Children.length; ++i) {
    json.Children[i].parent = json;

    addParent(json.Children[i]);
  }
}

function indexPaths(json, prefix) {
  if (prefix) prefix += "/";
  prefix = prefix || "";
  prefix += json.Name;

  global.pathIndex[prefix] = json;
  json.NodePath = prefix;

  for (var i = 0; i < json.Children.length; ++i) {
    indexPaths(json.Children[i], prefix);
  }
}

function findLinkReferences(json) {
  if (json.LinkPath) {
    var linked = global.pathIndex[json.LinkPath];
    if (!linked) assertThrow("Link '" + json.LinkPath + "' from " + json.Name + " doesn't exist");

    linked.ReverseLinks = linked.ReverseLinks || [];
    linked.ReverseLinks.push(json.NodePath);
  }

  for (var i = 0; i < json.Children.length; ++i) {
    findLinkReferences(json.Children[i]);
  }
}

function drawToc(json) {
  var ul = create("ul");
  $("toc").appendChild(ul);

  drawTocHelper(json, ul);

  $("bytes").style.marginLeft = $("toc").scrollWidth + 20 + "px";
}

function drawTocHelper(o, parentUL) {
  var li = create("li", {textContent: o.Name, onclick: makeOnClick(o)});

  global.allTocLI.push(li);

  o.tocDom = li;

  parentUL.appendChild(li);

  var ch = o.Children;

  if (ch.length) {
    var ul = create("ul");
    global.allTocUL.push(ul);
    for (var i = 0; i < ch.length; ++i) {
      drawTocHelper(ch[i], ul);
    }
    parentUL.appendChild(ul);
  }
}

function createBasicDetailsDOM(parent, o) {
  var details = create("div", {onclick: makeOnClick(o)});

  details.appendChild(create("p", {textContent: o.Name}));
  details.appendChild(create("p", {textContent: o.Value}));

  parent.appendChild(details);

  return details;
}

function findErrors(o) {
  for (var i = 0; i < o.Errors.length; ++i) {
    var errorDiv = createBasicDetailsDOM($("details"), o);
    errorDiv.classList.add("error");

    errorDiv.appendChild(create("p", {textContent: o.Errors[i]}));
  }

  for (i = 0; i < o.Children.length; ++i) {
    findErrors(o.Children[i]);
  }
}

function ToHex(code, width) {
  const hexEncodeArray = [
    "0",
    "1",
    "2",
    "3",
    "4",
    "5",
    "6",
    "7",
    "8",
    "9",
    "A",
    "B",
    "C",
    "D",
    "E",
    "F",
  ];

  var s = "";
  for (var i = 0; i < width || code; ++i) {
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
  var el = $(id);
  while (el.firstChild) el.removeChild(el.firstChild);
}

function removeErrorDetails() {
  var details = $("details");
  while (details.firstElementChild.nextElementSibling)
    details.removeChild(details.firstElementChild.nextElementSibling);
}

function cleanupDisplay() {
  global = {
    pathIndex: {}, // maps path url to node
    allTocUL: [], // all Table of Contents Unordered List DOM elements
    allTocLI: [], // all Table of Contents List Item DOM elements
  };
  removeChildren("tocSearch");
  removeChildren("toc");
  removeChildren("bytes");
  removeChildren("focusDetail");
  removeErrorDetails();
}

function displayHex(bytes) {
  var div = $("bytes");
  var width = 16;
  var size = bytes.length;

  var rowLabelWidth = ToHex(size - 1).length;

  div.appendChild(create("code", {textContent: Array(rowLabelWidth + 1).join("-") + "    "}));

  for (var i = 0; i < width; i++) {
    div.appendChild(create("code", {textContent: ToHex(i, 2) + " "}));
  }

  div.appendChild(create("br"));

  for (var j = 0; j < size; j += width) {
    div.appendChild(create("code", {textContent: ToHex(j, rowLabelWidth) + "    "}));

    for (i = j; i - j < width; i++) {
      div.appendChild(
        create("code", {
          textContent: ToHex(bytes[i], 2) + " ",
          id: byteID(i),
        })
      );
    }

    div.appendChild(create("code", {innerHTML: "&nbsp;&nbsp;&nbsp;&nbsp;"}));

    for (i = j; i - j < width; i++) {
      var lit = font[bytes[i]];
      div.appendChild(create("code", {textContent: lit, id: litID(i)}));
    }

    div.appendChild(create("br"));
  }
}

function displayParse(json) {
  addParent(json);
  indexPaths(json);
  findLinkReferences(json);
  drawToc(json);
  findErrors(json);

  window.onhashchange = makeOnHashChange(json);

  $("tocSearch").oninput = Search;

  if (window.location.href.indexOf("#") === -1) {
    setFocusObject(json);
  } else {
    window.onhashchange();
  }
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
  var time = document.createElement("span");
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

      var fileReader = new FileReader();
      fileReader.onload = () => bytesCallBack(new Uint8Array(this.result));
      fileReader.readAsArrayBuffer(file);
    })
  );
}

function fileChange(evt, pollMS, fileCallback) {
  /** @type {FileList} */
  var files = evt.target.files;
  var f = files[0];

  fileCallback(f);
  /** @type {Date} */
  var lastModified = f.lastModifiedDate;

  setInterval(() => {
    if (f.lastModifiedDate.getTime() !== lastModified.getTime()) {
      lastModified = f.lastModifiedDate;
      fileCallback(f);
    }
  }, pollMS);
}

function readExampleBytes(callback) {
  var file = "Program.dat";

  var req = new XMLHttpRequest();
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
  var file = "bytes.json";

  var req = new XMLHttpRequest();
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
  var req = new XMLHttpRequest();
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

var exampleButton = $("example");
var fileInput = $("fileInput");

/*
  If URL query is blank:
  - Wait for the user to input a file
  - Post file to get JSON form parse 
  If URL has ?Example
  - Get EXE from Program.dat
  - Get JSON parse from bytes.json
*/

if (window.location.href.indexOf("?Example=true") === -1) {
  exampleButton.onclick = () => {
    var hash = window.location.href.split("#")[1] || "";
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
//TODO visualize all link, link targets
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
