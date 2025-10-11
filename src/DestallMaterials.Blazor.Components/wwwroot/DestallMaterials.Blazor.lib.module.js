window.scrollElement_X = function (elementId, x) {
    const elem = document.getElementById(elementId);
    elem.scrollTo(x, 0);
}

window.scrollElement_Y = function (elementId, y) {
    const elem = document.getElementById(elementId);
    elem.scrollTo(0, y);
}

window.Y_elementDeviation = function (elemId, containerId) {
    const elem = document.getElementById(elemId);
    const container = document.getElementById(containerId);

    const style = getComputedStyle(container);

    const rect = container.getBoundingClientRect();
    const elemRect = elem.getBoundingClientRect();

    const paddingTop = parseFloat(style.paddingTop) + parseFloat(style.webkitMarginBefore);
    const paddingBottom = parseFloat(style.paddingBottom) + parseFloat(style.webkitMarginAfter);

    const isElemBelowTop = (rect.top + paddingTop) - elemRect.top;
    const isElemAboveBottom = elemRect.bottom - (rect.bottom - paddingBottom);

    if (isElemBelowTop <= 0 && isElemAboveBottom <= 0) {
        return 0;
    }
    if (isElemBelowTop <= 0 && isElemAboveBottom > 0) {
        return -isElemAboveBottom;
    }
    if (isElemBelowTop > 0 && isElemAboveBottom <= 0) {
        return isElemBelowTop;
    }
    return isElemBelowTop - isElemAboveBottom;
}

window.X_elementDeviation = function (elemId, containerId) {
    const elem = document.getElementById(elemId);
    const container = document.getElementById(containerId);

    const style = getComputedStyle(container);

    const paddingLeft = parseFloat(style.paddingLeft);
    const paddingRight = parseFloat(style.paddingRight);

    const rect = container.getBoundingClientRect();
    const elemRect = elem.getBoundingClientRect();

    const isElemRighter = (rect.left + paddingLeft) - elemRect.left;
    const isElemLefter = elemRect.right - (rect.right - paddingRight);

    if (isElemRighter <= 0 && isElemLefter <= 0) {
        return 0;
    }
    if (isElemRighter <= 0 && isElemLefter > 0) {
        return -isElemLefter;
    }
    if (isElemRighter > 0 && isElemLefter <= 0) {
        return isElemRighter;
    }
    return isElemRighter - isElemLefter;
}

window.Y_scrollToFit = function (elemId, containerId) {
    const element = document.getElementById(elemId);

    element.scrollIntoView({ behavior: "smooth", block: "start" });
}

window.setCssVariableValue = function (elemId, variableName, value) {
    const elem = document.getElementById(elemId);
    if (elem) {
        elem.style.setProperty(variableName, value);
    }
}

window.getItemScroll_Y = function (elementId) {
    const elem = document.getElementById(elementId);
    if (elem) {
        const result = elem.scrollTop;
        return result;
    }
}

window.disableDefaultHandling = function (elemId, eventType) {
    const elem = document.getElementById(elemId);
    if (elem) {
        elem[eventType] = event => {
            event.preventDefault();
        }
        elem.removeEventListener(eventType, elem[eventType]);
    }
}

const gatherWindowScrollState = () => {
    const height = window.innerHeight;
    const width = window.innerWidth;

    const scrolledVertically = elem.scrollY;
    const scrolledHorizontally = elem.scrollX;

    const maxVerticalScroll = elem.scrollHeight;
    const maxHorizontalScroll = elem.scrollWidth;

    return { height, width, scrolledVertically, scrolledHorizontally, maxVerticalScroll, maxHorizontalScroll };
};

const getWindowScrollState = () => {
    const windowScrollState = {
        height: window.innerHeight,
        width: window.innerWidth,
        scrolledVertically: window.screenY,
        scrolledHorizontally: window.screenX,
        maxVerticalScroll: window.innerHeight,
        maxHorizontalScroll: window.innerWidth
    };

    return [
        windowScrollState.height,
        windowScrollState.width,
        windowScrollState.scrolledVertically,
        windowScrollState.scrolledHorizontally,
        windowScrollState.maxVerticalScroll,
        windowScrollState.maxHorizontalScroll,
        0,
        0
    ];
}


const destallMaterials = {
    sensors: {
        scroll: {
            subscribe: (elemId, service) => {

                if (elemId === '__window') {
                    const elem = window;
                    const initialCallback = elem.onscroll || (() => { });
                    const addedCallback = async () => {
                        await service.invokeMethodAsync('ReactToScrollEventAsync',
                            elemId,
                            getWindowScrollState()
                        );
                    };

                    window.onscroll = async () => {
                        initialCallback();
                        await addedCallback();
                    };

                    return 1;
                }
                else {
                    const elem = document.getElementById(elemId);
                    const formerOnScroll = elem.onscroll || (() => { });

                    if (elem == null) {
                        return 0;
                    }

                    elem.onscroll = async () => {
                        formerOnScroll();

                        const rect = elem.getBoundingClientRect();

                        const height = elem.clientHeight;
                        const width = elem.clientWidth;

                        const scrolledVertically = elem.scrollTop;
                        const scrolledHorizontally = elem.scrollLeft;

                        const maxVerticalScroll = elem.scrollHeight;
                        const maxHorizontalScroll = elem.scrollWidth;

                        await service.invokeMethodAsync('ReactAsync',
                            elemId,
                            [
                                height,
                                width,
                                scrolledVertically,
                                scrolledHorizontally,
                                maxVerticalScroll,
                                maxHorizontalScroll,
                                rect.left,
                                rect.top
                            ]
                        );
                    }

                    return 1;
                }
            }
        },
        resize: {
            subscribe: (elemId, service) => {
                let elem;
                if (elemId === '__window') {
                    elem = window;
                }
                else {
                    elem = document.getElementById(elemId);
                }

                if (!elem) {
                    return 0;
                }

                const resizeObserver = new ResizeObserver(async () => {
                    const height = elem.clientHeight;
                    const width = elem.clientWidth;

                    await service.invokeMethodAsync('ReactAsync', elemId, [height, width])
                });

                resizeObserver.observe(elem);

                return 1;
            } 
        }
    },
    uiManipulation: {
        getBoundingRectangle: (elemId) => {
            const elem = document.getElementById(elemId);
            const r = elem.getBoundingClientRect();

            return [r.top, r.bottom, r.left, r.right, r.width, r.height];
        }
    }
};

window.destallMaterials = destallMaterials;