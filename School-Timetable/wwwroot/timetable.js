const script = document.createElement('script');
script.src = '_content/School-Timetable/interact.min.js';
script.onload = function() {
    const customScript = document.createElement('script');
    customScript.src = '_content/School-Timetable/dragDrop.js';
    document.body.appendChild(customScript);
};
document.body.appendChild(script);