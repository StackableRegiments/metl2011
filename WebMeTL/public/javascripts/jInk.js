(function(contexts){
    var id = "metl_ink_generated_canvas"
    Commands.add("joinedConversation",function(conversation){
        $('#'+id).remove()
    })
    Commands.add("slideRendered", function(details){
        var img = details.img
        var onComplete = function(){
            var context = $(img)  
        /*Canvas seems to need to be initialized with dimensions already in place or it behaves oddly with scaling*/
            var canvas = $("<canvas id='"+id+"' width='"+img.naturalWidth+"px' height='"+img.naturalWidth+"px'></canvas>").css({left:0,top:0}).css("position","absolute").css("z-index",9)
            context.after(canvas)
            var pen = canvas[ 0 ].getContext( "2d" );  
            var lastPenPoint = null;
            var isIPhone = 
                (new RegExp( "iPhone", "i" )).test(navigator.userAgent) || (new RegExp( "iPad", "i" )).test(navigator.userAgent)
            var getCanvasLocalCoordinates = function( pageX, pageY ){
                var position = canvas.offset();
                return({
                    x: (pageX  - position.left),
                    y: (pageY - position.top)
                });
            };
            var getTouchEvent = function( event ){
                if(isIPhone){
                    if(window.event.targetTouches.length > 1){
                        return false //A gesture, not for us.
                    }
                    return window.event.targetTouches[ 0 ] 
                }
                return event
            };
            var onTouchStart = function( event ){
                var touch = getTouchEvent( event );
                if(!touch) return true; //We didn't know how to handle this.
                var localPosition = getCanvasLocalCoordinates(
                    touch.pageX, 
                    touch.pageY 
                );
                lastPenPoint = { 
                    x: localPosition.x, 
                    y: localPosition.y
                };
                pen.beginPath(); 
                pen.moveTo( lastPenPoint.x, lastPenPoint.y );
                canvas.bind(
                    (isIPhone ? "touchmove" : "mousemove"),
                    onTouchMove
                );
                canvas.bind(
                    (isIPhone ? "touchend" : "mouseup"),
                    onTouchEnd
                );
                return false; //We handled this.  We don't want the browser to.
            };
            var onTouchMove = function( event ){
                var touch = getTouchEvent( event );
                var localPosition = getCanvasLocalCoordinates(
                    touch.pageX, 
                    touch.pageY 
                );
                lastPenPoint = { 
                    x: localPosition.x, 
                    y: localPosition.y
                };
                pen.lineTo( lastPenPoint.x, lastPenPoint.y );		
                pen.stroke();		
                return false;
            };
            var onTouchEnd = function( event ){
                canvas.unbind(
                    (isIPhone ? "touchmove" : "mousemove")
                );
                canvas.unbind(
                    (isIPhone ? "touchend" : "mouseup")
                );
                canvas.width = window.innerWidth;
                canvas.height = window.innerHeight;
                return false;
            };
            canvas.bind(
                (isIPhone ? "touchstart" : "mousedown"),
                function( event ){
                    return onTouchStart( event );
                }
            );
        }
    if(!img.complete)
        img.onload = onComplete
    else
        onComplete()
    })
})()
