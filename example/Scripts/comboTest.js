
YUI().use('node', 'event', 'scrollview', 'tabview', function (Y) {
    Y.one('#main h2').setContent('Welcome to YUI 3!');

    var tabview = new Y.TabView({
        children: [{
            label: 'foo',
            content: '<p>foo content</p>'
        }, {
            label: 'bar',
            content: '<p>bar content</p>'
        }, {
            label: 'baz',
            content: '<p>baz content</p>'
        }]
    });

    tabview.render('#tabs');
    tabview.selectChild(2);

});