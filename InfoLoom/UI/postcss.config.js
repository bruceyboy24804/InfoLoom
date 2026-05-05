var colorConverter = require("postcss-color-converter");

module.exports = {
    syntax: "postcss-scss",
    plugins: [
        colorConverter({
            outputColorFormat: "rgb",
            ignore: ["hex"],
        }),
    ],
};