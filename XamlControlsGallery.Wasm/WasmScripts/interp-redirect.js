let isIOS = !!navigator.platform && /iPad|iPhone|iPod/.test(navigator.platform);
let isProduction = window.location.hostname.toLowerCase().indexOf("platform.uno") !== -1;

var parts = window.location.hostname.split('.');

if (isProduction && parts.length > 0) {
    let isInterpreted = window.location.hostname.toLowerCase().indexOf("-i") !== -1;

    if (isIOS && !isInterpreted) {
        window.location.href = "https://xamlcontrolsgallery-i.platform.uno";
    }

    if (!isIOS && isInterpreted) {
        window.location.href = "https://xamlcontrolsgallery.platform.uno";
    }
}