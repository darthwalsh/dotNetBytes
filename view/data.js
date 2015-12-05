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
        
function readBytes(file, callback)
{
  var oReq = new XMLHttpRequest();
  oReq.open("GET", file);
  oReq.responseType = "arraybuffer";
  
  oReq.onload = function() {
    if (this.status == 200 && oReq.response) {
      var arr = new Uint8Array(oReq.response);
      
      var hex = "";
      var lit = "";
      
      for (var i = 0; i < arr.byteLength; i++) {
          var code = arr[i];
          hex += hexEncodeArray[code >>> 4];
          hex += hexEncodeArray[code & 0x0F];
          hex += " ";
          
          lit += String.fromCharCode(code);
          
          if (i % 16  == 15) {
            hex += "      ";
            hex += lit.replace(/[\x00-\x1F\x7F-\x9F]/g, " ");
            lit = "";
            
            hex += "\n";
          }
      }
      
      callback(hex);
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

window.onload = function() {
  var div = $("div");

  readBytes("AddR.exe", function(s) {
    var a = create("pre");
    a.innerText = s;

    div.appendChild(a);
    
    readJson("bytes.json", function(json) {
      var n = json.Name;
      console.log(n);
    });
  });
};