    /*
    *React componenet for rendering a searchable list of documents/attachments
    */
var SearchableDocList = React.createClass({
    getInitialState: function(){
        return {
            items: this.props.items
        };
    },

    /*
    *Function receives newly uploaded document by
    *callback from AttachmentInput.jsx etc
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
    *to traverse document items list for name and description containing input
    *and set new state with only those items
    */
    onSearchChange: function (event) {
        var newDocuments = [];

        var value = event.target.value;
        var documents = this.props.items;
        for (var i = 0; i < documents.length; i++) {
            var currentDocument = documents[i];

            for (var j = 0; j < currentDocument.documentVersions.length; j++) {
                var version = currentDocument.documentVersions[j];
                if (version.name.toLowerCase().indexOf(value.toLowerCase()) > -1 || (version.description != null && version.description.toLowerCase().indexOf(value.toLowerCase()) > -1)) {
                    newDocuments.push(currentDocument);
                }
            }
        }
        this.setState({
            items: newDocuments
        });
    },

    /*
    *Function renders heading, Attachment Input component for uploading files, and DocListDisplay for diplaying list of documents
    * and document search input
    */
    render: function () {
        var placeholderText = "Search " + this.props.searchType + "s";
        return (
          <div>
              <div className="indigo subtitle z-depth-1 row">
                <h4 className="white-text s12">Attachments</h4>
              </div>
              <AttachmentInput details={this.props.details} pageType={this.props.pageType} addItemToState={this.addItemToState} />
              <div className="row">
                  <div className="input-field col s12 m6 l4">
                      <i className="material-icons prefix">search</i>
                      <input type="text" className="form-control" placeholder={placeholderText} onChange={this.onSearchChange} />
                  </div>
              </div> 
              <div>
                  <DocListDisplay items={this.state.items} pageType={this.props.pageType} searchType={this.props.searchType} />
              </div>
          </div>
      );
    },
});

    /*
    *React Component that renders a list of all versions of a document from a list of documents
    *All document versions are pushed to displayItems list which is rendered as a materialize css collection
    */
var DocListDisplay = React.createClass({
    render: function(){
        var documentsList = this.props.items;
        var listOfVersions = [];
        var displayItems = [];

        if(documentsList)
        {
            for (var i = 0; i < documentsList.length; i++) {
                var document = documentsList[i];
                var versionsList = document.documentVersions;
                for (var j = 0; j < versionsList.length; j++) {
                    listOfVersions.push(versionsList[j]);
                }
            }
            for (var i = 0; i < listOfVersions.length; i++) {
                displayItems.push(<DocVersion key={i} item={listOfVersions[i]} pageType={this.props.pageType} searchType={this.props.searchType } />);
            }
        }

        return (
            <div>
                <ul className="collection">
                    {displayItems}
                </ul>
            </div>
            );
}
});
    /*
    *React component displays information about document version and provides download link to azure blob storage container
    */
var DocVersion = React.createClass({

    /*
    *FUNCTION NOT IN USE - currently directly link to azure blob is used in render, without ajax
    *TODO: allow for secure ajax call to blob download method in server side controller
    */
    Download: function () {

        var blobPath = this.props.item.url;

        $.ajax({
            type: 'POST',
            url: '/' + this.props.pageType + '/DownloadAttachment',
            data: { 'blobPath': blobPath },
            success: function (data) {
                $('body').html(data);
            }

        })

    },
    /*
    * Function renders colleciton items consisting of item name and description and direct link to document in azure blob storage
    */
    render: function () {
        var item = this.props.item;
        return(
                <li className="collection-item avatar">
                         <i className="material-icons circle">description</i>
                         <span className="title">{item.name}</span>
                          <p>
                              {item.description}
                          </p>
                         <a href={appGlobals.blobUrl + item.url} className="secondary-content"><i className="material-icons">play_for_work</i></a>
                </li>
            )
    }

});
