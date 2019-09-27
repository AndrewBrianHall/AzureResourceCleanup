function copyTextToClipboard(elementName) {
    var textToCopy = document.getElementById(elementName)
    var copyText = document.getElementById('contentToCopy');
    copyText.value = textToCopy.innerText;
    copyText.select();
    copyText.setSelectionRange(0, 99999); /*For mobile devices*/

    /* Copy the text inside the text field */
    document.execCommand("copy");

    /* Alert the copied text */
    alert("Copied the text: " + copyText.value);
}