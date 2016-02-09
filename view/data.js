"use strict";

function $(id) { return document.getElementById(id); }
function create(el) { return document.createElement(el); }

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
    case 0: r = v; g = t; b = p; break;
    case 1: r = q; g = v; b = p; break;
    case 2: r = p; g = v; b = t; break;
    case 3: r = p; g = q; b = v; break;
    case 4: r = t; g = p; b = v; break;
    case 5: r = v; g = p; b = q; break;
  }
  r = Math.round(r * 255);
  g = Math.round(g * 255);
  b = Math.round(b * 255);
  return "#"+(r).toString(16)+(g).toString(16)+(b).toString(16);
}

function getColor(n) {
  return HSVtoRGB(n / 12, 0.8, 1);
}

function getDimColor(n) {
  return HSVtoRGB(n / 12, 0.3, 1);
}

var hexEncodeArray = [
  '0', '1', '2', '3', '4', '5', '6', '7',
  '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
];

function assertThrow(message) {
  debugger;
  alert(message);
  throw message;
}
        
function readBytes(file, callback)
{
  var oReq = new XMLHttpRequest();
  oReq.open("GET", file);
  oReq.responseType = "arraybuffer";
  
  oReq.onload = function() {
    if (this.status == 200 && oReq.response) {
      callback(new Uint8Array(oReq.response));
    } else {
      assertThrow("Couldn't find " + file)
    }
  };
  
  oReq.send();
}

function readJson(file, callback)
{
  var oReq = new XMLHttpRequest();
  oReq.open("GET", file);
  
  oReq.onload = function() {
    if (this.status == 200 && oReq.responseText) {
      callback(JSON.parse(oReq.responseText));
    } else {
      assertThrow("Couldn't find " + file)
    }
  };
  
  oReq.send();
}

function byteID(i) {
  return "byte" + i;
}
function litID(i) {
  return "lit" + i;
}

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
  
  var firstBox = first.getBoundingClientRect()
  var lastBox = last.getBoundingClientRect()
  
  // Need to scroll up
  if (lastBox.bottom < 0) {
    first.scrollIntoView( /* top: */ true);
  }
  
  // Need to scroll down
  if (firstBox.top > window.innerHeight) {
    last.scrollIntoView( /* top: */ false);
  }
}

function Search() {
  var text = $("tocSearch").value.toLowerCase();
    
  // Hide all the ToC
  for (var i = 0; i < allTocUL.length; ++i) {
    allTocUL[i].style.display = "none";
  }
  for (var i = 0; i < allTocLI.length; ++i) {
    allTocLI[i].style.display = "none";
  }
  
  // Unhide all the ToC up the parents
  var tocDiv = $("toc");
  for (var i = 0; i < allTocLI.length; ++i) {
    var li = allTocLI[i];
    if (li.textContent.toLowerCase().indexOf(text) === -1)
      continue;
    
    while (li !== tocDiv) {
      li.style.display = "";
      if (li.previousElementSibling)
        li.previousElementSibling.style.display = "";
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
  for (var i = 0; i < allTocUL.length; ++i) {
    allTocUL[i].style.display = "none";
  }
  // Unhid all the text elements
  for (var i = 0; i < allTocLI.length; ++i) {
    allTocLI[i].style.display = "";
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
  for (var i = 0; i < allTocLI.length; ++i) {
    allTocLI[i].style.textDecoration = "";
  }
  toc.style.textDecoration = "underline"
  
  // Reset all the byte display
  var grandparent = o;
  while (grandparent.parent)
    grandparent = grandparent.parent;
  for (i = grandparent.Start; i < grandparent.End; ++i) {
    setByte(i, "white", null, "auto");
  }
  
  setFocusHelper(o);
  
  scrollIntoView(o);
  
  drawDetails(o); 
}

function drawDetails(o) {
  var focusDetail = $("focusDetail");
  while (focusDetail.firstChild)
    focusDetail.removeChild(focusDetail.firstChild);
  
  var detailDiv = createBasicDetailsDOM(focusDetail, o);
  var description = create("p");
  description.textContent = o.Description;
  detailDiv.appendChild(description);
  
  if (o.ReverseLinks) {
    var referencesText = create("p");
    referencesText.textContent = "Referenced by:";
    detailDiv.appendChild(referencesText);
    
    var ul = create("ul");
    for (var i = 0; i < o.ReverseLinks.length; ++i) {
      var li = create("li");
      
      var a = create("a");
      a.setAttribute("href", "#" + o.ReverseLinks[i]);
      var matchingPrefix = 0;
      for (; matchingPrefix < o.ReverseLinks[i].length && matchingPrefix < o.NodePath.length; ++matchingPrefix) {
        if (o.NodePath[matchingPrefix] !== o.ReverseLinks[i][matchingPrefix])
          break;
      }
      a.textContent = o.ReverseLinks[i].substring(matchingPrefix);
      li.appendChild(a);
            
      ul.appendChild(li);
    }
    detailDiv.appendChild(ul);
  }
}

function makeOnClick(o) {
  return function(ev) {
    setFocusObject(o);
  };
}

function makeOnHashChange(json) {
  return function (ev) {
    var hash = window.location.href.split("#")[1];
    var names = hash.split("/");

    var o = json;
    for (var i = 1; i < names.length; ++i) {
      for (var chi = 0; ; ++chi) {
        if (chi == o.Children.length) {
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
      onclick = function(ev) { window.location.hash = o.LinkPath; };
      cursor = "pointer";
    } 
    for (var i = o.Start; i < o.End; ++i) {
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

var pathIndex = {};

function indexPaths(json, prefix) {
  if (prefix) prefix += "/";
  prefix = prefix || "";
  prefix += json.Name;
  
  pathIndex[prefix] = json;
  json.NodePath = prefix;
  
  for (var i = 0; i < json.Children.length; ++i) {
    indexPaths(json.Children[i], prefix);
  }
}

function findLinkReferences(json) {
  if (json.LinkPath) {
    var linked = pathIndex[json.LinkPath];
    if (!linked)
      assertThrow("Link '" + json.LinkPath + "' from " + json.Name + " doesn't exist");
    
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

var allTocUL = [];
var allTocLI = [];

function drawTocHelper(o, parentUL) {
  var li = create("li");
  allTocLI.push(li);
  li.textContent = o.Name;
  li.onclick = makeOnClick(o);
  
  o.tocDom = li;
  
  parentUL.appendChild(li);
  
  var ch = o.Children; 
  
  if (ch.length) {
    var ul = create("ul");
    allTocUL.push(ul);
    for (var i = 0; i < ch.length; ++i) {
      drawTocHelper(ch[i], ul);
    }
    parentUL.appendChild(ul);
  }
}

function createBasicDetailsDOM(parent, o) {
  var details = create("div");
  
  details.onclick = makeOnClick(o);
  
  var p = create("p");
  p.textContent = o.Name;
  details.appendChild(p);
  
  p = create("p");
  p.textContent = o.Value;
  details.appendChild(p);
  
  parent.appendChild(details);
  
  return details;
}

function findErrors(o) {
  for (var i = 0; i < o.Errors.length; ++i) {
    var errorDiv = createBasicDetailsDOM($("details"), o);
    errorDiv.classList.add("error");
    
    var p = create("p");
    p.textContent = o.Errors[i];
    errorDiv.appendChild(p);
  }
  
  for (var i = 0; i < o.Children.length; ++i) {
    findErrors(o.Children[i]);
  }
}

function ToHex(code, width) {
  var s = "";
  for (var i = 0; i < width || code; ++i) {
    s = hexEncodeArray[code & 0x0F] + s;
    code >>>= 4;
  }
  
  return s;
}

window.onload = function() {
  var div = $("bytes");
  var width = 16;
  
  readBytes("Program.dat", function(arr) {
    var rowLabelWidth = ToHex(arr.byteLength - 1).length;
    
    var corner = create("code");
    corner.textContent = Array(rowLabelWidth + 1).join("-") + "    ";
    div.appendChild(corner);
  
    for (var i = 0; i < width; i++) {
      var col = create("code");
      col.textContent = ToHex(i, 2) + " ";
      div.appendChild(col);
    }
    
    div.appendChild(create("br"));
    
    for (var j = 0; j < arr.byteLength; j += width) {
      var rowLabel = create("code");
      rowLabel.textContent = ToHex(j, rowLabelWidth) + "    ";
      div.appendChild(rowLabel);
      
      for (var i = j; i - j < width; i++) {
        var a = create("code");
        a.textContent = ToHex(arr[i], 2) + " ";
        a.setAttribute("id", byteID(i));
        div.appendChild(a);
      }
      
      var sp = create("code");
      sp.innerHTML = "&nbsp;&nbsp;&nbsp;&nbsp;";
      div.appendChild(sp);
      
      for (var i = j; i - j < width; i++) {
        var lit = String.fromCharCode(arr[i]);
        var ll = create("code");
        ll.textContent = lit.replace(/[\x00-\x1F\x7F-\x9F]/g, ".");
        ll.setAttribute("id", litID(i));
        div.appendChild(ll);
      }
      
      div.appendChild(create("br"));
    }
    
    readJson("bytes.json", function(json) {
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
    });
  });
};

//TODO smart colors
//TODO hover preview
//TODO keyboarding through ToC
//TODO favorites "shortcut" pinning
//TODO details pinning + clearing
