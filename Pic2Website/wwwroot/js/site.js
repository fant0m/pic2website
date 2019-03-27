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

    $('.ajax-form').on('submit', function (e) {
        $('#error').addClass('d-none');
        $('#loading').modal('show');

        e.preventDefault();
        $.ajax({
            type: "POST",
            url: "/",
            data: new FormData($(this)[0]),
            processData: false,
            contentType: false,
            success: function (data) {
                if (data) {
                    if (data.key == 'error') {
                        $('#error').text(data.value).removeClass('d-none');
                        $("html, body").animate({ scrollTop: 0 }, 600);
                    } else if (data.key == 'success') {
                        window.location.href = "/Result/" + data.value;
                    }
                }

                $('#loading').modal('hide');
            },
            error: function () {
                $("html, body").animate({ scrollTop: 0 }, 600);
                $('#error').text('Whoops. Server is busy please try again later.').removeClass('d-none');
                $('#loading').modal('hide');
            }
        });
    });
});
