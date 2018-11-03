// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.

$(document).ready(function () {
    $('#form').on('submit', function (e) {
        e.preventDefault();
        $.get("/generate", $(this).serialize(), function (data) {
            var d = new Date();
            $("#img").attr("src", "/images/output.png?" + d.getTime());
        });
    });
});