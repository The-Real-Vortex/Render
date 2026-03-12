window.infiniteScroll = {
    observer: null,
    initialize: function (dotNetHelper, elementId) {
        const element = document.getElementById(elementId);
        if (!element) return;

        if (this.observer) this.observer.disconnect();

        this.observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting) {
                dotNetHelper.invokeMethodAsync('LoadMoreData');
            }
        }, {
            root: null,
            rootMargin: '5000px',
            threshold: 0.1
        });

        this.observer.observe(element);
    }
};

window.cookieManager = {
    getCookie: function (name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) {
            return parts.pop().split(';').shift();
        }
        return null;
    },

    setCookie: function (name, value, days) {
        let expires = "";
        if (days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = `; expires=${date.toUTCString()}`;      }
        document.cookie = `${name}=${value}${expires}; path=/; SameSite=Strict`;
    },

    deleteCookie: function (name) {
        document.cookie = `${name}=; expires=Thu, 01 Jan 2000 00:00:00 UTC; path=/;`;
    }
};