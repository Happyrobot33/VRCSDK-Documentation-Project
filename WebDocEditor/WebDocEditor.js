var parser = new DOMParser();

//on xml_input change
function xml_input_change() {
    let xml_input = document.getElementById("xml_input");
    let xml = xml_input.value;
    xmlDoc = parser.parseFromString(xml,"text/xml");

    //print to console
    console.log(xmlDoc);
    return xmlDoc;
}

function updateOutput(){
    console.log("updateOutput");
    let output = document.getElementById("xml_output");
    let xml = "<members>";
    for(let i = 0; i < memberObjects.length; i++){
        xml += memberObjects[i].getXML();
    }
    xml += "</members>";
    output.value = xml;
}

let memberObjects = new Array();

function populateEditor(xmlDoc, editorRoot){
    if (xmlDoc === null){
        return;
    }
    //clear the editor root
    editorRoot.innerHTML = "";

    //loop over each member of the xml document
    let members = xmlDoc.getElementsByTagName("member");
    //clear the members array
    memberObjects = new Array();
    for(let i = 0; i < members.length; i++){
        let member = members[i];
        let newMember = new Member(member);

        //add the member to the members array
        memberObjects.push(newMember);

        //append the member div to the editor root
        editorRoot.appendChild(newMember.getDiv());
    }
}

const cleanerRegex = /\n +/g;

class Parameter{
    name;
    summary;

    constructor(xml){
        this.name = xml.getAttribute("name");
        this.summary = xml.textContent;
    }

    getDiv(){
        let div = document.createElement("div");
        div.classList.add("parameter_section");
        EditableField(div, "name", this.name);
        EditableField(div, "summary", this.summary);
        return div;
    }

    getXML(){
        let xml = "<param name=\"" + this.name + "\">" + this.summary + "</param>";
        return xml;
    }
}

class Member{
    name;
    URL;
    summary;
    remarks;
    parameters;
    incomplete;

    constructor(xml){
        this.name = xml.getAttribute("name");
        //inner node called docURL
        this.URL = xml.getElementsByTagName("docURL")[0] ? xml.getElementsByTagName("docURL")[0].textContent : "";
        //inner node called summary
        this.summary = xml.getElementsByTagName("summary")[0] ? xml.getElementsByTagName("summary")[0].textContent : "";
        //inner node called remarks
        this.remarks = xml.getElementsByTagName("remarks")[0] ? xml.getElementsByTagName("remarks")[0].textContent : "";
        this.incomplete = xml.getElementsByTagName("incomplete")[0] ? true : false;

        //clean up all new lines, including whitespace
        this.URL = this.URL.replace(cleanerRegex, " ").trim();
        this.summary = this.summary.replace(cleanerRegex, " ").trim();
        this.remarks = this.remarks.replace(cleanerRegex, " ").trim();

        //parameters
        this.parameters = new Array();
        let parameters = xml.getElementsByTagName("param");
        for(let i = 0; i < parameters.length; i++){
            let param = parameters[i];
            this.parameters.push(new Parameter(param));
        }

        console.log(this);
    }

    //decorated div
    getDiv(){
        //we want a text area for the link, summary, and remarks
        let div = document.createElement("div");
        div.classList.add("member");
        div.innerHTML = "<h2>" + this.name + "</h2>";
        EditableField(div, "docURL", this.URL);
        EditableField(div, "summary", this.summary);
        EditableField(div, "remarks", this.remarks);

        //parameters
        let parametersDiv = document.createElement("div");
        parametersDiv.classList.add("parameters");
        for(let i = 0; i < this.parameters.length; i++){
            parametersDiv.appendChild(this.parameters[i].getDiv());
        }
        div.appendChild(parametersDiv);
        return div;
    }

    getXML(){
        let xml = "<member name=\"" + this.name + "\">";
        //only add it if it exists
        //xml += "<docURL>" + this.URL + "</docURL>";
        xml += this.URL ? "<docURL>" + this.URL + "</docURL>" : "";
        xml += this.incomplete ? "<incomplete />" : "";
        xml += this.summary ? "<summary>" + this.summary + "</summary>" : "";
        xml += this.remarks ? "<remarks>" + this.remarks + "</remarks>" : "";
        for(let i = 0; i < this.parameters.length; i++){
            xml += this.parameters[i].getXML();
        }
        xml += "</member>";
        return xml;
    }
}

function EditableField(div, fieldName, fieldValue){
    //make a div to put the label and textarea in
    let fieldDiv = document.createElement("div");
    fieldDiv.classList.add("node");
    //div.innerHTML += "<label for='" + fieldName + "'>" + fieldName + ":</label>";
    //div.innerHTML += "<textarea id='" + fieldName + "' rows='4' cols='50'>" + fieldValue + "</textarea>";
    let content = document.createElement("div");
    content.contentEditable = true;
    fieldDiv.appendChild(content);
    content.innerHTML = fieldValue;
    //create the label
    let label = document.createElement("label");
    label.htmlFor = fieldName;
    label.innerHTML = fieldName + ":";
    fieldDiv.insertBefore(label, content);
    content.addEventListener("input", updateOutput);
    div.appendChild(fieldDiv);
}

//initial setup
window.onload = function(){
    let xml_input = document.getElementById("xml_input");
    xml_input.addEventListener("input", function(){
        //get the div with the id editor
        let editorRoot = document.getElementById("editor");
        xml_input_change();
        populateEditor(xml_input_change(), editorRoot);
    });
 };
