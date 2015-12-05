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
  if (n == 0)
    return "white";

  var h = (Math.log2(n) % 12) / 12;
    return HSVtoRGB(h, 0.5, 0.7);
}

var hexEncodeArray = [
  '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
];
        
function readTextFile(file, callback)
{
  var oReq = new XMLHttpRequest();
  oReq.open("GET", file);
  oReq.responseType = "arraybuffer";
  
  oReq.onload = function() {
    if (this.status == 200 && oReq.response) {
      var arr = new Uint8Array(oReq.response);
      
      var s = "";
      
      for (var i = 0; i < arr.byteLength; i++) {
          var code = arr[i];
          s += hexEncodeArray[code >>> 4];
          s += hexEncodeArray[code & 0x0F];
          s += " ";
          
          if (i % 16  == 15)
            s += "\n";
      }
      
      callback(s);
    } else {
      alert("Couldn't find " + file)
    }
  };
  
  oReq.send();
}
  
window.onload = function() {
  var div = $("div");

  readTextFile("AddR.exe", function(s) {
    var a = create("pre");
    a.innerText = s;

    div.appendChild(a);
  });
};