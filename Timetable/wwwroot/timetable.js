const script = document.createElement('script');
script.src = '_content/Timetable/interact.min.js';
script.onload = function() {
    const customScript = document.createElement('script');
    customScript.src = '_content/Timetable/dragDrop.js';
    document.body.appendChild(customScript);
};
document.body.appendChild(script);