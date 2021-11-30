/**
 * TODO(API) write docs
 * Use like <input is="file-watcher" id="fileInput" type="file" />
 *    <!-- <input is="file-watcher" type="file" onChange="console.log" /> -->
 *
 * TODO(API) test fallback behavior gives regular onChange vent
 * 
 * TODO(API) the choose file text isn't changing 
 * maybe use https://stackoverflow.com/a/32919644/771768
 */

class FileWatcher extends HTMLInputElement {
  constructor() {
    super();

    this.addEventListener("click", async event => {
      event.preventDefault();

      const [fileHandle] = await window.showOpenFilePicker(); //TODO(API) cancel

      this.getFile = () => fileHandle.getFile();
      //TODO(API) clear the old interval if set
      setInterval(() => this.poll(), 500);
    });

    this.addEventListener("drop", async event => {
      event.preventDefault();

      const [item] = event.dataTransfer.items;
      const handle = await item.getAsFileSystemHandle();

      this.getFile = () => handle.getFile();
      setInterval(() => this.poll(), 500);
    });

    this.ondragover = event => {
      event.preventDefault();
      return false;
    };
  }

  async poll() {
    /** @type {File} */
    const file = await this.getFile();
    if (
      !this.lastModifiedDate ||
      this.lastModifiedDate.getTime() != file.lastModifiedDate.getTime()
    ) {
      this.lastModifiedDate = file.lastModifiedDate;
      const event = new Event("change");
      event.bytes = file; //TODO(API) API should mirror an non-custom <input>
      this.dispatchEvent(event);
    }
  }
}

customElements.define("file-watcher", FileWatcher, {extends: "input"});
