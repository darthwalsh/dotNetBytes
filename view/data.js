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
  return HSVtoRGB(n / 12, 0.9, 1);
}

function getDimColor(n) {
  return HSVtoRGB(n / 12, 0.3, 1);
}

var hexEncodeArray = [
  '0', '1', '2', '3', '4', '5', '6', '7',
  '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
];
        
function readBytes(file, callback)
{
  var oReq = new XMLHttpRequest();
  oReq.open("GET", file);
  oReq.responseType = "arraybuffer";
  
  oReq.onload = function() {
    if (this.status == 200 && oReq.response) {
      callback(new Uint8Array(oReq.response));
    } else {
      alert("Couldn't find " + file)
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
      alert("Couldn't find " + file)
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

function setColor(i, color) {
  $(byteID(i)).style.backgroundColor = color;
  $(litID(i)).style.backgroundColor = color;
}

function setOnClick(i, onclick) {
  $(byteID(i)).onclick = onclick;
  $(litID(i)).onclick = onclick;
}

function setFocus(o)
{
  var toc = $("toc");
  while (toc.hasChildNodes())
    toc.removeChild(toc.firstChild); 
    
  setFocusHelper(o);
}

function makeOnClick(o) {
  return function(ev) {
    setFocus(o);
  };
}

function setFocusHelper(o, currentChild) {  
  var toc = $("toc");
  
  var parentLI = null;
  if (o.parent) {
    parentLI = setFocusHelper(o.parent, o);
  } else {
    var ul = create("ul");
    var li = create("li");
    li.innerText = "All";
    li.onclick = makeOnClick(o);
    
    ul.appendChild(li);
    toc.appendChild(ul);
    parentLI = li;
  }
  
  var ch = o.Children;
  
  for (var chI = 0; chI < ch.length; ++chI) {
    var col;
    if (ch[chI] === currentChild) {
      col = getColor(chI);
    } else {
      col = getDimColor(chI);
    }
    
    for (var i = ch[chI].Start; i < ch[chI].End; ++i) {
      setColor(i, col);
      setOnClick(i, makeOnClick(ch[chI]));
    }
  }
  
  var childLI = null;
  ul = create("ul");
  for (var chI = 0; chI < ch.length; ++chI) {
    li = create("li");
    li.innerText = ch[chI].Name;
    li.onclick = makeOnClick(ch[chI]);
    
    ul.appendChild(li);
    
    if (ch[chI] === currentChild) {
      childLI = li;
    }
  }
  
  if (parentLI) {
    parentLI.parentElement.insertBefore(ul, parentLI.nextSibling);
  } else {
    toc.appendChild(ul);
  }
  
  return childLI;
}

function addParent(json) {
  for (var i = 0; i < json.Children.length; ++i) {
    json.Children[i].parent = json;
    
    addParent(json.Children[i]);
  }
}

window.onload = function() {
  var div = $("bytes");
  var width = 16;
  
  readBytes("AddR.exe", function(arr) {
    for (var j = 0; j < arr.byteLength; j += width) {
      for (var i = j; i - j < width; i++) {
        var code = arr[i];
        var hex = hexEncodeArray[code >>> 4];
        hex += hexEncodeArray[code & 0x0F];
        hex += " ";
        
        var a = create("code");
        a.innerText = hex;
        a.setAttribute("id", byteID(i));
        div.appendChild(a);
      }
      
      var sp = create("code");
      sp.innerHTML = "&nbsp;&nbsp;&nbsp;&nbsp;";
      div.appendChild(sp);
      
      for (var i = j; i - j < width; i++) {
        code = arr[i];
        var lit = String.fromCharCode(code);
        var ll = create("code");
        ll.innerText = lit.replace(/[\x00-\x1F\x7F-\x9F]/g, ".");
        ll.setAttribute("id", litID(i));
        div.appendChild(ll);
      }
      
      div.appendChild(create("br"));
    }
    
    readJson("bytes.json", function(json) {
      addParent(json);
      setFocus(json);
    });
  });
};