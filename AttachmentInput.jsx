// Copyright Daniel Stepanenko

//React component for file input box used for document attachments
var AttachmentInput = React.createClass({
    getInitialState: function () {
        return {
            details: this.props.details,
            isLoading: false
        };
    },

    /*
    *Function called on input change in render function
    *Packages input file and posts for server by ajax
    *Receives DocumentInfo and DocumentVersions response from server
    *and calls back to addItemToState in parent component for immediate display
    */
    uploadFile: function (e) {
        var component = this;

        var data = new FormData();
        var files = e.target.files;

        if (files.length > 0) {
            data.append(files[0].name, files[0]);
            data.append("filename", files[0].name);
            data.append("id", this.props.details.id);

            this.setState({
                isLoading: true
            });
        }

        $.ajax({
            type: 'POST',
            url: '/' + this.props.pageType + '/UploadDocument?',
            processData: false,
            contentType: false,
            data: data,
            success: function (data) {
                console.log('Doc Sent successfully')
                this.setState({
                    isLoading: false
                });

                var fileName = data['name'];
                var filePath = data['path'];
                var versionid = data['versionid'];

                var documentInfoId = data['docid']
                var documentVersions = [];

                documentVersions.push({
                    description: null,
                    documentVersionId: versionid,
                    name: fileName,
                    uploadDate: null,
                    uploadedBy: null,
                    url: filePath,
                    versionNumber: null
                });

                this.props.addItemToState({
                    documentInfoId: documentInfoId,
                    documentVersions: documentVersions
                });

            }.bind(this),
            error: function (xhr, status, err) {
                console.error(this.props.submitUrl, status, err.toString());
                console.warn(xhr.responseText)
            }.bind(this)
        })
    },

    /*
    * Render function renders preloader spinner, input field and button
    */
    render: function () {
        var preloader;

        if (this.state.isLoading) {
            preloader = (
                  <div className="preloader-wrapper small active">
                    <div className="spinner-layer spinner-green-only">
                      <div className="circle-clipper left">
                        <div className="circle"></div>
                      </div><div className="gap-patch">
                        <div className="circle"></div>
                      </div><div className="circle-clipper right">
                        <div className="circle"></div>
                      </div>
                    </div>
                  </div>
                );
        }

        return (
               <div className="row">
                    <div className="col s12 m6 l4">
                      <div className="file-field input-field">

                        <div className="btn">
                          <span>File</span>
                          <input type="file" onChange={this.uploadFile} />
                        </div>

                        <div className="file-path-wrapper">
                          <input className="file-path validate" type="text" placeholder="Upload one or more files" />
                        </div>

                      </div>
                    </div>
                   <div>
                       {preloader}
                   </div>
               </div>
            );
    }
});