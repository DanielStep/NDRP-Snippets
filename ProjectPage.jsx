var ProjectPage = React.createClass({

    render: function () {
        var project = this.props.project;
        var projectId = project.projectDetails.id
        var propNames = ["name", "description", "refNumber"];

        //****SiteCreateMain component assigned to form variable to be passed down to Model through SearchableList prop*****
        var form = <SiteCreateMain submitUrl="/Sites/Create" id={projectId} />;

      return (
    <div>
          <EditableTitleBox propNames={["name", "description", "refNumber"]} details={project.projectDetails} saveUrl="/Project/SaveProjectDetails" editType="Project" showImage={true} />
           <SearchableList items={project.sites} form={form} id={this.props.project.projectDetails.id} urlCreateForm="../../Site/Create/" urlSubIndex="/Site/Index/" searchType="Site" />
           <SearchableDocList items={project.documents} searchType="Document" pageType="Project" details={project.projectDetails} />

    </div>
    );
  }
});

ReactDOM.render(<ProjectPage project={project} />, document.getElementById('main'));
