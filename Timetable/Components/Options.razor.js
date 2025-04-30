export async function downloadFileFromStream (fileName, contentStreamReference) {
    try {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer], { type: 'application/octet-stream' });
        const url = URL.createObjectURL(blob);

        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        document.body.appendChild(anchorElement);
        anchorElement.click();
        document.body.removeChild(anchorElement);
        URL.revokeObjectURL(url);
    } catch (error) {
        console.error('Error while exporting to file:', error);
    }
}

export function promptFileSelect(dotNetRef, maxSize, allowedExtensions) {
    const inp = document.createElement('input');
    inp.type = 'file';
    if (allowedExtensions && allowedExtensions.length)
        inp.accept = allowedExtensions.map(e => '.' + e).join(',');

    inp.onchange = async ev => {
        const file = ev.target.files[0];
        const buf = await file.arrayBuffer();
        const u8 = new Uint8Array(buf);

        let binary = '';
        for (let i = 0; i < u8.length; i++) {
            binary += String.fromCharCode(u8[i]);
        }
        const base64 = btoa(binary);
        dotNetRef.invokeMethodAsync('ReceiveFileBase64', base64);
    };
    inp.click();
}
