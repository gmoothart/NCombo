
YUI().use('node',
          'event',
          'scrollview',
          'tabview',
          'yui2-calendar',
          'aui-calendar',
function (Y) {
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

    //
    // alloy UI calendar
    //

    var alloyCalendar = new Y.Calendar({
        trigger: '#alloyCalendar',
        dates: ['09/14/2009', '09/15/2009'],
        dateFormat: '%d/%m/%y %A',
        setValue: true,
        selectMultipleDates: true
    }).render();
    
    //
    // yui2 calendar
    //
    var YAHOO = Y.YUI2;
    cal1 = new YAHOO.widget.Calendar('yui2Calendar');
    cal1.render();
});