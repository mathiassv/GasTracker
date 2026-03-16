window.gtTheme = {
    apply: function (theme) {
        var effective = theme === 'system'
            ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
            : theme;
        document.documentElement.setAttribute('data-bs-theme', effective);
        localStorage.setItem('gt-theme', theme);
    },
    get: function () {
        return localStorage.getItem('gt-theme') || 'light';
    }
};
