/// <binding AfterBuild='swagger-doc-comments' />

/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');

gulp.task('swagger-doc-comments', function () {
    gulp.src(['./bin/Debug/netcoreapp1.0/AdventureWorksWebApi.xml', './bin/Release/netcoreapp1.0/AdventureWorksWebApi.xml'])
    .pipe(gulp.dest('./'))
});