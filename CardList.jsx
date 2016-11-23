    //Copyright Daniel Stepanenko
    
    /*
    *React component which displays list Project, Site, or Audit entities
    *as materialize css cards in index pages
    */
var SearchableList = React.createClass({
    getInitialState: function(){
        return {
            items: this.props.items
        };
    },
    componentDidMount: function(){
      $(document).ready(function(){
        Materialize.updateTextFields();
      });
    },

    /*
    *Function receives newly created Project, Site or Audit by
    *callback from CreateProjectMain.jsx etc
    *Updates component state with new entity for display
    */
    addItemToState: function (newItem) {

        var newItems = [];
        newItems = this.state.items;
        newItems.push(newItem)

        this.setState({
            items: newItems
        });
    },

    /*
    *Function called on change of search input in render function
    *to traverse card items list for name and description containing input
    *and set new state with only those items
    */
    onSearchChange: function(event){
        var value = event.target.value;
        var items = this.props.items;
        var newItems = [];
        for(var i=0; i< items.length; i++){
            if (items[i].name.toLowerCase().indexOf(value.toLowerCase()) > -1 || (items[i].description != null && items[i].description.toLowerCase().indexOf(value.toLowerCase()) > -1)) {
                newItems.push(items[i]);
            }
        }
        this.setState({
            items: newItems
        });
    },

    //***CreateSiteMain form passed on to Modal component vias props here****
    /*
    *Render function appends addItemtoState function to Form component in props and passes it down to Modal component
    *Renders card list, search box and modal
    */
    render: function () {
        var placeholderText = "Search " + this.props.searchType + "s";

        var clonedElementWithMoreProps = React.cloneElement(this.props.form, { addItemToState: this.addItemToState });

        return (
          <div className="col s12">
              <div className="indigo subtitle z-depth-1 row">
                <h4 className="white-text s12">{this.props.searchType}s</h4>
              </div>
              <div className="row">

                <div className="input-field col s12 m6 l4">
                 <i className="material-icons prefix">search</i>
                 <input id="searchbox" type="text" onChange={this.onSearchChange}></input>
                 <label htmlFor="searchbox">Search {this.props.searchType}s</label>
               </div>
              </div>
              <div className="col s12">
                  <ListDisplay items={this.state.items} urlSubIndex={this.props.urlSubIndex} searchType={this.props.searchType} />
              </div>

              <Modal id={this.props.id} addItemToState={this.addItemToState} form={clonedElementWithMoreProps}/>

          </div>
      );
    },
});

    /*
    *React component for displaying list of card items
    */
var ListDisplay = React.createClass({
    render: function(){
        var items = this.props.items;
        var displayItems = [];
        for(var i=0; i< items.length; i++){
            displayItems.push(<ItemCard key={i} item={items[i]} urlSubIndex={this.props.urlSubIndex} searchType={this.props.searchType} />);
        }

        return (
            <div className="row">
                <div>
                    {displayItems}
                </div>
            </div>
            );
}
});
    /*
    *React component for rendering a card with item info list of Projects/Sites/Audits
    *Renders associated image if present, alert notificaiton, href, name and description
    */
var ItemCard = React.createClass({
    render: function(){
        var item = this.props.item;
        var subIndexUrl = this.props.urlSubIndex + item.id.toString();
        var imagePath = 'http://materializecss.com/images/sample-1.jpg';
        if (item.imagePath != null && item.imagePath.length > 0) {
            imagePath = appGlobals.blobUrl + item.imagePath;
        }
        var notification;
        if (item.notificationNumber && item.notificationNumber > 0){
          notification = <div title="Number of High Risk Items" className="card-notification red-text"><i className="material-icons red-text">&#xE000;</i><span className="notification-text">{item.notificationNumber}</span></div>;
        }
        return (

                <div className="col s12 m4">
                    <a href={subIndexUrl}>
                    <div className="card medium hoverable sticky-action">
                        <div className="card-image waves-effect waves-block waves-light">
                            <img src={imagePath} />
                        </div>
                        <div className="card-content">
                            <h5>{item.name}</h5>{notification}
                            <p className="truncate">{item.description}</p>
                        </div>
                        <div className="card-action">
                            <span>Open {this.props.searchType}</span>
                        </div>
                    </div>
                    </a>
                </div>

        );
    }
});
